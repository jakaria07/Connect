import { inject, Injectable } from '@angular/core';
import { AuthService } from '@abp/ng.core';
import * as signalR from '@microsoft/signalr';
import { ReplaySubject } from 'rxjs';
import { MessageDto } from './chat-http.service';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ChatSignalrService {
  private readonly auth = inject(AuthService);

  private connection: signalR.HubConnection | null = null;
  private readonly messageReceivedSubject = new ReplaySubject<MessageDto>(1);
  messageReceived$ = this.messageReceivedSubject.asObservable();

  currentConversationId: string | null = null;

  async start(): Promise<void> {
    if (this.connection) return;

    const baseUrl = environment.apis.default.url.replace(/\/$/, '');

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/signalr/chat`, {
        accessTokenFactory: async () => {
          const token = this.auth.getAccessToken();
          return token || '';
        },
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('MessageReceived', (message: MessageDto) => {
      this.messageReceivedSubject.next(message);
    });

    await this.connection.start();
  }

  async stop(): Promise<void> {
    if (!this.connection) return;
    await this.connection.stop();
    this.connection = null;
    this.currentConversationId = null;
  }

  async joinConversation(conversationId: string): Promise<void> {
    if (!this.connection) {
      await this.start();
    }

    await this.connection!.invoke('JoinConversation', conversationId);
    this.currentConversationId = conversationId;
  }

  async leaveConversation(conversationId: string): Promise<void> {
    if (!this.connection) return;

    await this.connection.invoke('LeaveConversation', conversationId);
    if (this.currentConversationId === conversationId) {
      this.currentConversationId = null;
    }
  }
}
