import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '@abp/ng.core';
import { Subject, takeUntil } from 'rxjs';
import { ChatHttpService, ConversationDto, MessageDto } from '../services/chat-http.service';
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

  conversations: ConversationDto[] = [];
  selectedConversation: ConversationDto | null = null;
  messages: MessageDto[] = [];
  messageText = '';

  async ngOnInit(): Promise<void> {
    if (!this.auth.isAuthenticated) {
      this.auth.navigateToLogin();
      return;
    }

    this.conversations = await this.chatHttp.getMyConversations();

    this.chatSignalr.messageReceived$
      .pipe(takeUntil(this.destroy$))
      .subscribe(m => {
        if (this.selectedConversation && this.chatSignalr.currentConversationId === this.selectedConversation.id) {
          this.messages = [m, ...this.messages];
        }
      });

    await this.chatSignalr.start();
  }

  async selectConversation(c: ConversationDto): Promise<void> {
    if (this.selectedConversation?.id === c.id) {
      return;
    }

    if (this.selectedConversation) {
      await this.chatSignalr.leaveConversation(this.selectedConversation.id);
    }

    this.selectedConversation = c;
    this.messages = await this.chatHttp.getMessages(c.id, 0, 50);

    await this.chatSignalr.joinConversation(c.id);
  }

  async send(): Promise<void> {
    if (!this.selectedConversation) return;
    const text = this.messageText.trim();
    if (!text) return;

    const message = await this.chatHttp.sendMessage(this.selectedConversation.id, text);
    this.messages = [message, ...this.messages];
    this.messageText = '';
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    void this.chatSignalr.stop();
  }
}
