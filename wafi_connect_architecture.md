# Wafi.Connect — ABP + Angular + PostgreSQL + SignalR Architecture

This is a clean, layered ABP architecture for 1:1 real-time chat with PostgreSQL and SignalR, scoped for rapid delivery while remaining extensible.

---

## PART 1: PROJECT STRUCTURE

### 1.1 Solution layout (backend + separate Angular)

- **`Wafi.Connect.sln`**
  - **`src/`**
    - **`Wafi.Connect.Domain.Shared/`**
    - **`Wafi.Connect.Domain/`**
    - **`Wafi.Connect.Application.Contracts/`**
    - **`Wafi.Connect.Application/`**
    - **`Wafi.Connect.EntityFrameworkCore/`**
    - **`Wafi.Connect.HttpApi/`**
    - **`Wafi.Connect.HttpApi.Client/`** (optional)
    - **`Wafi.Connect.DbMigrator/`**
    - **`Wafi.Connect.Host/`** (ASP.NET Core startup host)
  - **`angular/`**
    - **`Wafi.Connect.Angular/`** (ABP Angular workspace)

### 1.2 Dependency rules (strict)

- Domain.Shared: no dependencies on other solution projects.
- Domain: depends on Domain.Shared only.
- Application.Contracts: depends on Domain.Shared.
- Application: depends on Application.Contracts, Domain, Domain.Shared.
- EntityFrameworkCore: depends on Domain, Domain.Shared.
- HttpApi: depends on Application.Contracts (and Application via DI).
- Angular: separate; uses generated proxies.

### 1.3 Why each layer exists

- Domain: business invariants and aggregates.
- Application: use-case orchestration and DTOs.
- EF Core: persistence mapping.
- HttpApi: transport and SignalR hub.
- Angular: UI and real-time client.

---

## PART 2: DOMAIN DESIGN

### 2.1 Aggregates

- **`Conversation`** (Aggregate Root)
  - `Id` (Guid)
  - `User1Id` (Guid)
  - `User2Id` (Guid)
  - `CreationTime`
  - `IsArchived` (bool, optional)

- **`Message`** (Entity within `Conversation`)
  - `Id` (Guid)
  - `ConversationId` (Guid)
  - `SenderUserId` (Guid)
  - `Text` (string)
  - `CreationTime`
  - Navigation property: `Conversation`

### 2.2 Domain rules

- A message can be sent only if the sender is either User1 or User2 of the conversation.
- No duplicate conversations: always store the smaller Guid as User1Id and the larger Guid as User2Id; enforce with a unique index on (User1Id, User2Id).

### 2.3 Repository interfaces (Domain)

- `IConversationRepository`
  - `GetByUserPairAsync(Guid userA, Guid userB)`
  - `GetUserConversationsAsync(Guid userId)`
- `IMessageRepository`
  - `GetByConversationAsync(Guid conversationId, int skip, int take)`

---

## PART 3: APPLICATION LAYER

### 3.1 Application services

- **`ConversationAppService`**
  - `CreateConversationAsync(CreateConversationDto)`
  - `GetMyConversationsAsync()`

CreateConversationAsync flow:

- Order user IDs (smaller Guid → User1Id, larger Guid → User2Id)
- Check repository for existing conversation by ordered pair
- If exists → return existing conversation
- If not → create new conversation

- **`MessageAppService`**
  - `SendMessageAsync(SendMessageDto)`
  - `GetMessagesAsync(GetMessagesDto)`

### 3.2 DTOs (Application.Contracts)

- `CreateConversationDto`: `OtherUserId`
- `ConversationDto`: `Id`, `OtherUserId`, `CreationTime`, `IsArchived`
- `SendMessageDto`: `ConversationId`, `Text`
- `MessageDto`: `Id`, `SenderUserId`, `Text`, `CreationTime`
- `GetMessagesDto`: `ConversationId`, `Skip`, `Take`

### 3.3 Validation

- Basic DataAnnotations on DTOs.
- Business rule checks in application services before calling domain.

### 3.4 Authorization

- Use ABP permissions; require authenticated users.
- Validate membership in `SendMessageAsync`.

### 3.5 Mapping

- AutoMapper profiles in Application layer.

---

## PART 4: INFRASTRUCTURE (PostgreSQL + EF Core)

### 4.1 EF Core setup

- Use `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Configure connection string per environment.

### 4.2 DbContext and mappings

- Map `Conversation` and `Message` tables in `snake_case`.
- No complex value objects; keep entities simple.

### 4.3 Indexes

- `Conversations(User1Id)`
- `Conversations(User2Id)`
- `Messages(ConversationId, CreationTime desc)`
- Unique index on (User1Id, User2Id) to prevent duplicate conversations.

### 4.4 Migrations

- Code-first migrations in `EntityFrameworkCore`.
- Apply via `DbMigrator` or host startup.

---

## PART 5: HTTP API LAYER

### 5.1 Controllers

- Thin controllers calling application services.
- Conventional controllers via ABP where convenient.

### 5.2 Swagger

- Enable OpenAPI with JWT auth.

---

## PART 6: SIGNALR INTEGRATION

### 6.1 Hub location

- Place `ChatHub` in `HttpApi` (or Host).

### 6.2 Hub responsibilities

- `JoinConversation(conversationId)`
- `LeaveConversation(conversationId)`
- Authentication via ABP token.
- Before adding a connection to a conversation group, validate that the current user belongs to that conversation.

### 6.3 Real-time push

- In `MessageAppService.SendMessageAsync`:
  - Save message.
  - Inject `IHubContext<ChatHub>`.
  - `await hubContext.Clients.Group($"conversation-{conversationId}").SendAsync("MessageReceived", messageDto);`

### 6.4 Scaling

- Single-instance only; no backplane.

---

## PART 7: ANGULAR ARCHITECTURE

### 7.1 Folder structure

- `src/app/`
  - `core/` (auth, SignalR service, interceptors)
  - `shared/` (common components)
  - `features/chat/`
    - `conversation-list/`
    - `chat-window/`
    - `chat.service` (HTTP)
    - `signalr.service`

### 7.2 Core module

- Auth token handling.
- HTTP interceptors.
- SignalR connection lifecycle.

### 7.3 Feature modules

- `ChatModule` with components and services.

### 7.4 Services

- `ChatService` (HTTP API calls).
- `SignalrService` (connects, joins/leaves groups, receives `MessageReceived`).

### 7.5 State management

- Simple service + RxJS; no NgRx.

---

## PART 8: DEVELOPMENT ROADMAP

### Phase 1 — Scaffold
- ABP CLI: create layered solution with Angular.
- Switch provider to PostgreSQL.

### Phase 2 — Domain + EF Core
- Define `Conversation` and `Message`.
- Add EF Core mappings and indexes.
- Create and apply migrations.

### Phase 3 — Application + API
- Implement `ConversationAppService` and `MessageAppService`.
- Add DTOs and validation.
- Expose controllers.

### Phase 4 — SignalR
- Add `ChatHub`.
- Push from `MessageAppService` using `IHubContext`.

### Phase 5 — Angular UI
- Implement conversation list and chat window.
- Add HTTP and SignalR services.
- Wire up real-time updates.

### Phase 6 — Demo
- Run migrations.
- Test end-to-end 1:1 chat flow.

---

## PART 9: BEST PRACTICES

- Keep layers clean; no EF Core in Domain.
- No domain events or outbox for now.
- Use indexes for pagination and lookups.
- Validate sender membership in application layer.
- Keep SignalR hub thin; push directly from app service.
- Angular: use ABP generated proxies; simple service state.

---

## Extension points

- Add group chat by evolving `Conversation`.
- Add outbox for guaranteed delivery.
- Add Redis backplane for scale-out.
- Add presence tracking if needed.
