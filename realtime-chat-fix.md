# Connect — Realtime Chat (SignalR) Fix: End‑to‑End Technical Recap

This document is a structured end-to-end recap of the **Connect** chat module and the exact steps taken to diagnose and fix the issue where **messages were not appearing in real time** between two open chat sessions.

---

## 1) Project Overview

### 1.1 Purpose
The chat module enables 1:1 conversations between users with:
- Conversation list management
- Message history loading
- Sending messages via HTTP API
- Realtime delivery via SignalR when both users have the conversation open

### 1.2 Tech Stack
- **Frontend**: Angular (standalone component), RxJS, `@microsoft/signalr`
- **Backend**: ASP.NET Core + **ABP Framework**, OpenIddict validation
- **Realtime Transport**: SignalR hub (`/signalr/chat`)

### 1.3 Runtime Topology / URLs
- **Angular**: `http://localhost:4200`
- **Backend API**: `https://localhost:44355`
- **SignalR Hub**: `https://localhost:44355/signalr/chat`

---

## 2) High-Level Architecture

### 2.1 Frontend responsibilities
- Load conversations/users via HTTP.
- Load messages for selected conversation via HTTP.
- Maintain an active SignalR connection.
- Join/leave the active conversation group.
- Update UI when `MessageReceived` arrives.

Key frontend files:
- `angular/src/app/chat/components/chat-page.component.ts`
- `angular/src/app/chat/services/chat-signalr.service.ts`
- `angular/src/app/chat/services/chat-http.service.ts`

### 2.2 Backend responsibilities
- Persist messages (HTTP controller → application layer).
- Notify conversation participants via SignalR group.
- Enforce that only conversation participants may join conversation groups.

Key backend files:
- `src/Wafi.Connect.HttpApi/SignalR/ChatHub.cs`
- `src/Wafi.Connect.HttpApi/SignalR/SignalRChatMessageNotifier.cs`
- `src/Wafi.Connect.HttpApi.Host/ConnectHttpApiHostModule.cs`

---

## 3) Runtime Data Flow (Happy Path)

### 3.1 Auth / token
- Angular obtains an access token via ABP auth.
- SignalR client uses `accessTokenFactory` to pass the token to the hub connection.

### 3.2 Loading chat state (HTTP)
1. `GET /api/chat/conversations/my` → conversation list
2. `GET /api/users` → user lookup
3. On conversation select: `GET /api/chat/messages?conversationId=...`

### 3.3 Joining conversation (SignalR group)
- Client calls hub method: `JoinConversation(conversationId)`
- Hub validates:
  - `conversationId` is a valid GUID
  - authenticated user is participant (`conversation.IsParticipant(userId)`)
- Hub adds the connection to SignalR group:
  - `conversation-{conversationId}`

### 3.4 Sending a message
- Sender uses HTTP:
  - `POST /api/chat/messages` (via `ChatHttpService.sendMessage`)
- Backend persists and triggers notifier.

### 3.5 Broadcasting + receiving
- Backend notifier sends:
  - `MessageReceived` event to group `conversation-{conversationId}`
  - payload `{ conversationId, message }`
- Angular receives event and updates UI if it matches the selected conversation.

---

## 4) Problem Statement (Original Issue)

### 4.1 Symptom
- When two users had the same conversation open in two tabs, **new messages did not appear** on the receiver side until a page reload.

### 4.2 Reproduction
1. Open Tab A (User A) and Tab B (User B).
2. Select the same conversation in both tabs.
3. Send message from Tab A.
4. Receiver (Tab B) did not update until refresh.

---

## 5) Root Cause Analysis (What was actually wrong)

### 5.1 Hub method invocation failed: `Unauthenticated`
The browser console showed the SignalR connection was established, but `JoinConversation` failed:
- `System.UnauthorizedAccessException: Unauthenticated.`

Meaning:
- The client connected to SignalR.
- But the hub method had no authenticated identity.
- Therefore the connection never joined the conversation group.
- Therefore broadcasts to the group never reached the client.

### 5.2 Why identity was missing even though token existed
SignalR commonly transmits the token via query string during WebSockets negotiation:
- `...?access_token=...`

In this project, authentication is handled by **OpenIddict validation** (ABP). Out of the box, the hub path was not reliably extracting/processing the token for `/signalr/chat`.

### 5.3 `Context.UserIdentifier` was empty
After token processing was confirmed working, `Context.UserIdentifier` still appeared empty. ABP/OpenIddict provides the user id in the JWT `sub` claim. SignalR’s `UserIdentifier` uses a specific claim mapping (often `NameIdentifier`) that was not populated here.

### 5.4 Sender-side duplicate messages
After realtime started working, the sender saw messages **twice**:
- Once added locally from the HTTP send response
- Once echoed back via SignalR broadcast

Receiver only saw it once (SignalR only), so duplicates were isolated to the sender.

---

## 6) Fixes Applied (Chronological & Minimal)

### 6.1 Backend — Ensure SignalR hub is reachable and mapped
**File**: `src/Wafi.Connect.HttpApi.Host/ConnectHttpApiHostModule.cs`

- Confirmed hub mapping at:
  - `endpoints.MapHub<ChatHub>("/signalr/chat")`

### 6.2 Backend — Hub method signature compatibility
**File**: `src/Wafi.Connect.HttpApi/SignalR/ChatHub.cs`

- Hub methods accept `string conversationId` and parse to `Guid`.
- This prevents issues where the client sends string IDs.

### 6.3 Backend — OpenIddict validation hook for SignalR query-string token
**File**: `src/Wafi.Connect.HttpApi.Host/ConnectHttpApiHostModule.cs`

Added an OpenIddict validation event handler:
- Intercepts authentication processing
- Reads `access_token` parameter
- Applies it only for `/signalr/chat`

This ensures the SignalR hub requests are authenticated even when the token is supplied as query string.

### 6.4 Backend — Extract current user id from JWT `sub` claim
**File**: `src/Wafi.Connect.HttpApi/SignalR/ChatHub.cs`

Instead of relying on `Context.UserIdentifier`, the hub uses:
- `sub` claim (GUID)

This makes participant checks deterministic:
- `conversation.IsParticipant(currentUserId)`

### 6.5 Frontend — Ensure UI updates when `MessageReceived` arrives
**File**: `angular/src/app/chat/components/chat-page.component.ts`

- Subscribe once to `chatSignalr.messageReceived$`.
- Filter by selected conversation.
- Run the handler inside Angular `NgZone`.

### 6.6 Frontend — Prevent sender-side duplicates
**File**: `angular/src/app/chat/components/chat-page.component.ts`

Because the sender adds the message from the HTTP response, the SignalR echo must be ignored for the sender.

Implementation:
- Extract current user id from the JWT token `sub` claim.
- If `payload.message.senderUserId === currentUserId`, ignore.

---

## 7) Final Behavior (What works now)

### 7.1 Correct realtime updates
- When both users have the conversation open, messages appear instantly.

### 7.2 Correct message counts
- Sender sees the message once.
- Receiver sees the message once.

---

## 8) Verification Checklist

### 8.1 Manual test
1. Start backend and Angular.
2. Open two browser tabs (two different users).
3. Select the same conversation.
4. Send a message in one tab.
5. Confirm the other tab updates instantly.
6. Confirm sender does not show duplicates.

### 8.2 Key “good” indicators
- `JoinConversation` succeeds (no Unauthenticated error).
- `MessageReceived` event fires on receiver.

---

## 9) Key Files Changed (What to read first)

### Frontend
- `angular/src/app/chat/components/chat-page.component.ts`
  - subscriptions
  - message dedupe
  - zone/change detection considerations

- `angular/src/app/chat/services/chat-signalr.service.ts`
  - hub connection
  - `accessTokenFactory`
  - join/leave

### Backend
- `src/Wafi.Connect.HttpApi/SignalR/ChatHub.cs`
  - group membership authorization
  - extracting user id from `sub`

- `src/Wafi.Connect.HttpApi/SignalR/SignalRChatMessageNotifier.cs`
  - group broadcast `MessageReceived`

- `src/Wafi.Connect.HttpApi.Host/ConnectHttpApiHostModule.cs`
  - hub mapping
  - OpenIddict validation customization for SignalR

---

## 10) Troubleshooting Cheatsheet

### 10.1 `JoinConversation` fails with `Unauthenticated`
- Verify SignalR client is sending token (accessTokenFactory returns non-empty).
- Verify OpenIddict validation processes query-string `access_token` for `/signalr/chat`.

### 10.2 `JoinConversation` fails with “not a member”
- Confirm user id extraction uses correct claim (`sub`).
- Confirm conversation participant logic aligns with `User1Id` / `User2Id`.

### 10.3 Realtime works but UI doesn’t update
- Ensure the subscription updates `messages` inside `NgZone`.
- Ensure the filter uses matching `conversationId` string.

### 10.4 Sender duplicates
- Ensure sender ignores SignalR echo using `senderUserId === currentUserId`.

---

## Appendix A — Current SignalR event contract

### Event name
- `MessageReceived`

### Payload
```json
{
  "conversationId": "<guid>",
  "message": {
    "id": "<guid>",
    "senderUserId": "<guid>",
    "text": "...",
    "creationTime": "<iso-date>"
  }
}
```
