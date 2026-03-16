import { CommonModule } from '@angular/common';
import { Component, OnInit, OnDestroy, ChangeDetectorRef, inject, NgZone } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '@abp/ng.core';
import { Subject, takeUntil } from 'rxjs';
import { ChatHttpService, ConversationDto, MessageDto, UserLookupDto } from '../services/chat-http.service';
import { ChatSignalrService } from '../services/chat-signalr.service';

@Component({
  selector: 'app-chat-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-page.component.html',
  styleUrls: ['./chat-page.component.scss'],
})
export class ChatPageComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();
  private readonly chatHttp = inject(ChatHttpService);
  private readonly chatSignalr = inject(ChatSignalrService);
  private readonly auth = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly zone = inject(NgZone);

  conversations: ConversationDto[] = [];
  selectedConversation: ConversationDto | null = null;
  messages: MessageDto[] = [];
  messageText = '';

  users: UserLookupDto[] = [];
  selectedUserId: string | null = null;
  loading = false;
  error: string | null = null;
  messageLoadError: string | null = null;

  async ngOnInit(): Promise<void> {
    if (!this.auth.isAuthenticated) {
      this.auth.navigateToLogin();
      return;
    }

    this.loading = true;
    this.error = null;
    try {
      const [conversations, users] = await Promise.all([
        this.chatHttp.getMyConversations(),
        this.chatHttp.getUsers(),
      ]);
      this.conversations = conversations;
      this.users = users;
      this.selectedUserId = null;
    } catch (e: any) {
      this.error = 'Failed to load conversations.';
    } finally {
      this.loading = false;
    }

    this.setupMessageReceivedSubscription();

    await this.chatSignalr.start();
  }

  private getCurrentUserId(): string | null {
    try {
      const token = this.auth.getAccessToken();
      if (!token) return null;
      
      // Parse JWT token to get user ID from 'sub' claim
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.sub || null;
    } catch {
      return null;
    }
  }

  private setupMessageReceivedSubscription(): void {
    this.chatSignalr.messageReceived$
      .pipe(takeUntil(this.destroy$))
      .subscribe(payload => {
        // Run inside Angular's zone to trigger change detection
        this.zone.run(() => {
          // Only process messages for the currently selected conversation
          if (this.selectedConversation && payload.conversationId === this.selectedConversation.id) {
            // Check if this message was sent by the current user
            // If so, don't add it (it's already added from the HTTP response)
            const currentUserId = this.getCurrentUserId();
            if (payload.message.senderUserId !== currentUserId) {
              console.log('Adding received message to UI:', payload.message);
              this.messages = [payload.message, ...this.messages];
            } else {
              console.log('Ignoring own message received via SignalR (already added via HTTP):', payload.message);
            }
          }
        });
      });
  }

  async startConversation(): Promise<void> {
    const otherUserId = this.selectedUserId;
    if (!otherUserId) return;

    this.loading = true;
    this.error = null;
    try {
      const conversation = await this.chatHttp.createConversation(otherUserId);
      const exists = this.conversations.some(c => c.id === conversation.id);
      if (!exists) {
        this.conversations = [conversation, ...this.conversations];
      }
      await this.selectConversation(conversation);
    } catch (e: any) {
      this.error = 'Failed to create conversation. Check the user id and try again.';
    } finally {
      this.loading = false;
    }
  }

  async selectConversation(c: ConversationDto): Promise<void> {
    if (this.selectedConversation?.id === c.id) {
      return;
    }

    if (this.selectedConversation) {
      await this.chatSignalr.leaveConversation(this.selectedConversation.id);
    }

    this.selectedConversation = c;
    this.loading = true;
    this.messageLoadError = null;
    try {
      this.messages = await this.chatHttp.getMessages(c.id, 0, 50);
      this.messageLoadError = null;
      try {
        await this.chatSignalr.joinConversation(c.id);
      } catch {
        // best-effort; do not block message display
      }
    } catch (e: any) {
      this.messageLoadError = 'Failed to load messages for this conversation.';
    } finally {
      this.loading = false;
    }
  }

  async send(): Promise<void> {
    if (!this.selectedConversation) return;
    const text = this.messageText.trim();
    if (!text) return;

    this.error = null;
    try {
      const message = await this.chatHttp.sendMessage(this.selectedConversation.id, text);
      this.messages = [message, ...this.messages];
      this.messageText = '';
    } catch (e: any) {
      this.error = 'Failed to send message.';
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    void this.chatSignalr.stop();
  }

  getConversationDisplayName(conversation: ConversationDto): string {
    const user = this.users.find(u => u.id === conversation.otherUserId);
    return user ? `${user.displayName} (${user.userName})` : conversation.otherUserId;
  }

  getSenderDisplayName(senderUserId: string): string {
    const user = this.users.find(u => u.id === senderUserId);
    if (user) return user.userName;

    if (this.selectedConversation?.otherUserId === senderUserId) {
      return this.getConversationDisplayName(this.selectedConversation);
    }

    return senderUserId;
  }

  toLocalDate(value: string): Date {
    // backend sends ISO string (UTC). Date pipe will display local time.
    return new Date(value);
  }
}
