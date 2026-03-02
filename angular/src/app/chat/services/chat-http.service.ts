import { inject, Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';

export interface ConversationDto {
  id: string;
  otherUserId: string;
  creationTime: string;
  isArchived: boolean;
}

export interface MessageDto {
  id: string;
  senderUserId: string;
  text: string;
  creationTime: string;
}

@Injectable({ providedIn: 'root' })
export class ChatHttpService {
  private readonly rest = inject(RestService);

  getMyConversations(): Promise<ConversationDto[]> {
    return this.rest.request<any, ConversationDto[]>({
      method: 'GET',
      url: '/api/chat/conversations/my',
    }, { apiName: 'default' }).toPromise();
  }

  sendMessage(conversationId: string, text: string): Promise<MessageDto> {
    return this.rest.request<any, MessageDto>({
      method: 'POST',
      url: '/api/chat/messages',
      body: { conversationId, text },
    }, { apiName: 'default' }).toPromise();
  }

  getMessages(conversationId: string, skip = 0, take = 20): Promise<MessageDto[]> {
    return this.rest.request<any, MessageDto[]>({
      method: 'GET',
      url: '/api/chat/messages',
      params: { conversationId, skip, take },
    }, { apiName: 'default' }).toPromise();
  }
}
