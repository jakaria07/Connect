# Add Outbox Pattern for Guaranteed Delivery (Chat Message Notifications)
This plan adds a minimal, industry-standard **outbox** to ensure message persistence and SignalR notifications are decoupled yet reliably executed using a transactional outbox + background delivery worker.

## Goals / Non‑Goals
### Goals
- **Guarantee notification delivery attempt**: once a message is stored, its corresponding realtime notification will be retried until successfully processed.
- Keep changes **minimal and safe**, aligned with ABP patterns (UoW, background workers/jobs, EF Core migrations).
- Provide **observability** (attempt count, last error, timestamps) and **operational controls** (batch size, polling interval).

### Non‑Goals (explicit)
- Guarantee that a client actually received/acknowledged the message (SignalR cannot guarantee end-client receipt without app-level acks).
- Multi-node, exactly-once delivery semantics (we will design for it, but initial implementation targets single-instance).

## Current Baseline (What exists today)
- Message persistence + immediate notification happens in the same request:
  - `MessageAppService.SendMessageAsync(...)` inserts `Message` then calls `_chatMessageNotifier.NotifyMessageReceivedAsync(...)`.
- SignalR notifier implementation:
  - `SignalRChatMessageNotifier` sends `MessageReceived` to group `conversation-{conversationId}`.
- EF Core and ABP background jobs module are already referenced at the EF layer:
  - `builder.ConfigureBackgroundJobs();` exists in `ConnectDbContext`.

## Proposed Architecture Change
### Before
HTTP request (SendMessage) 
- Insert message
- Immediately push SignalR

### After (Outbox)
HTTP request (SendMessage)
- Insert message
- Insert outbox record **in the same transaction**
- Return response immediately

Background worker (polling)
- Fetch pending outbox records
- Attempt SignalR push
- Mark as processed or schedule retry

## Design Details

### 1) Outbox Data Model (New Table)
Add a new entity/table, e.g. `chat_outbox_messages` (name can be adjusted to your naming convention).

**Recommended columns**
- `Id` (Guid, PK) — outbox record id
- `Type` (string) — e.g. `ChatMessageReceived`
- `AggregateId` (Guid) — conversationId (for filtering/debug)
- `Payload` (json/text) — serialized data to publish, e.g. `{ conversationId, messageDto }`
- `CreatedAt` (DateTime)
- `ProcessedAt` (DateTime?)
- `Status` (enum/int) — Pending / Processing / Processed / Failed
- `Attempts` (int)
- `NextAttemptAt` (DateTime?)
- `LastError` (string?)

**Indexes**
- `(Status, NextAttemptAt, CreatedAt)` for fast polling
- `AggregateId` (optional) for debugging

**Idempotency / duplicates**
- SignalR push is “at least once” from the server’s perspective.
- UI already prevents local duplicates; for cross-client duplicates, we rely on message `Id` uniqueness (client can dedupe if needed).

### 2) Outbox Write (Transactional)
Modify `MessageAppService.SendMessageAsync`:
- Keep membership validation as-is.
- Insert `Message`.
- Create and insert an outbox record containing:
  - conversationId
  - messageDto

**Critical invariant**
- Message insert + outbox insert must occur in the **same Unit of Work** so they commit atomically.

### 3) Delivery Worker (Background Processing)
Implement a background processor with conservative defaults:

**Mechanism options (choose 1 for minimal change)**
- Option A (Recommended): `AbpBackgroundWorker` (simple hosted worker, polling loop)
- Option B: `IBackgroundJobManager` per outbox item (more infra, usually for heavier workloads)

**Processing algorithm (polling)**
- Every `PollInterval` (e.g. 1s–3s in dev, 3s–10s in prod):
  - Fetch up to `BatchSize` pending items where:
    - `Status == Pending` AND (`NextAttemptAt` is null or <= now)
  - Mark them `Processing` (optimistic) or process with row locking (preferred)
  - For each item:
    - Deserialize payload
    - Call `IChatMessageNotifier.NotifyMessageReceivedAsync(...)`
    - On success: set `ProcessedAt`, `Status=Processed`
    - On failure: increment attempts, set `LastError`, compute `NextAttemptAt` (exponential backoff), set `Status=Pending` (or `Failed` if max attempts reached)

**Retry policy**
- Exponential backoff: `min(2^attempts * baseDelay, maxDelay)`
  - baseDelay: 1s
  - maxDelay: 60s
- `MaxAttempts`: 20 (configurable)

### 4) Concurrency & Safety
Initial implementation assumes **single backend instance**.

If multi-instance is introduced later:
- Add one of:
  - DB-level row locking (`FOR UPDATE SKIP LOCKED` in PostgreSQL)
  - ABP distributed lock around each batch

### 5) Configuration Knobs (appsettings)
Add settings (Host project):
- `ChatOutbox:Enabled` (bool)
- `ChatOutbox:PollIntervalMs` (int)
- `ChatOutbox:BatchSize` (int)
- `ChatOutbox:MaxAttempts` (int)
- `ChatOutbox:MaxBackoffSeconds` (int)

### 6) Observability
- Log per batch:
  - number fetched
  - number processed
  - number retried
- Log failures with outbox id, conversation id, attempts, and error.
- Optional: expose a simple admin endpoint later (not required now).

## Code Touchpoints (Expected Files)
### Backend
- `src/Wafi.Connect.Domain` or `src/Wafi.Connect.Domain.Shared`
  - New outbox entity (or place in Domain if you want it as part of domain model)

- `src/Wafi.Connect.EntityFrameworkCore/EntityFrameworkCore/ConnectDbContext.cs`
  - Add `DbSet<ChatOutboxMessage>` and mapping
  - Add indexes

- `src/Wafi.Connect.EntityFrameworkCore/Migrations/*`
  - New migration to create the outbox table

- `src/Wafi.Connect.Application/Chat/MessageAppService.cs`
  - Replace direct `NotifyMessageReceivedAsync` call with outbox insert
  - (Optionally keep direct call behind feature flag for staged rollout)

- `src/Wafi.Connect.HttpApi.Host`
  - Register and start the outbox background worker
  - Add config binding

### Frontend
- No required changes. (Optional later: client-side dedupe by message id if needed.)

## Rollout Strategy (Safe & Minimal)
1. **Phase 1 (Dual-write, optional)**
   - Write outbox + still send SignalR immediately (feature flag)
   - Validate outbox processing in parallel without affecting UX

2. **Phase 2 (Outbox-only delivery)**
   - Turn off immediate send and rely on outbox worker

3. **Backout plan**
   - Feature flag to revert to immediate send if any production issues appear

## Testing Plan
### Unit tests
- Outbox record created when sending message.
- Retry scheduling logic.

### Integration tests
- Simulate notifier failure (throw exception) and confirm outbox retries.
- Confirm processed items are marked and not re-processed.

### Manual tests
- Run app normally; verify realtime still works.
- Temporarily break SignalR notifier (throw) then restore; verify messages eventually deliver.

## Estimates (Engineering Time)
- **Design + implementation**: 6–10 hours
  - Outbox entity + EF mapping + migration: 2–3h
  - Worker + retry/backoff + config: 2–4h
  - Wire-up + feature flag: 1–2h
- **Testing + verification**: 3–6 hours
  - happy-path + failure-path testing, logs review

Total: **~1–2 working days** depending on desired robustness.

## Open Questions (Answer before implementation)
1. Do you want **dual-write (immediate + outbox)** for a safer rollout, or switch directly to outbox-only?
2. What is your target deployment topology?
   - single instance (current) vs multiple instances (future)
3. Should max retry time be bounded (e.g., stop after 24h) or truly indefinite?

## Rollout Strategy (Safe & Minimal)
This project will use **Dual-write** initially and a **bounded retry** strategy, assuming a **single backend instance**.

1. **Phase 1 (Dual-write; default ON)**
   - Persist message + persist outbox record in the same Unit of Work.
   - Continue sending SignalR immediately (current behavior) to keep UX unchanged.
   - Outbox worker also runs and attempts delivery; this validates the pipeline safely.
   - Control via feature flag/config:
     - `ChatOutbox:Enabled=true`
     - `ChatOutbox:ImmediatePublishEnabled=true`

2. **Phase 2 (Outbox-only)**
   - Disable immediate publish and rely only on the outbox worker:
     - `ChatOutbox:ImmediatePublishEnabled=false`
   - Keep the outbox write path unchanged.

3. **Backout plan**
   - Re-enable immediate publish at runtime via config if needed:
     - `ChatOutbox:ImmediatePublishEnabled=true`

## Testing Plan
### Unit tests
- Outbox record created when sending message.
- Retry scheduling logic.

### Integration tests
- Simulate notifier failure (throw exception) and confirm outbox retries.
- Confirm processed items are marked and not re-processed.

### Manual tests
- Run app normally; verify realtime still works.
- Temporarily break SignalR notifier (throw) then restore; verify messages eventually deliver.

## Estimates (Engineering Time)
- **Design + implementation**: 6–10 hours
  - Outbox entity + EF mapping + migration: 2–3h
  - Worker + retry/backoff + config: 2–4h
  - Wire-up + feature flag: 1–2h
- **Testing + verification**: 3–6 hours
  - happy-path + failure-path testing, logs review

Total: **~1–2 working days** depending on desired robustness.

## Open Questions (Answer before implementation)
The following decisions are now fixed for implementation:
1. **Rollout**: Dual-write first (immediate publish + outbox), then switch to outbox-only.
2. **Retries**: Bounded retries (stop after `MaxAttempts`, mark as Failed).
3. **Deployment**: Single backend instance.

Failed-item handling decision:
1. **Operations**: No admin endpoint initially; handle failed outbox items via DB/log inspection only.

Operational note (manual inspection / retry)
- Inspect failed items by filtering on `Status=Failed` (and/or `Attempts >= MaxAttempts`).
- To force a retry manually, update the record:
  - set `Status=Pending`
  - set `NextAttemptAt` to `now` (or `null`)
  - optionally clear `LastError`
- To permanently suppress an item, keep it as `Failed` (or archive/delete via DB procedure if you introduce one later).

## Suggested initial defaults (can be adjusted later)
- `ChatOutbox:Enabled=true`
- `ChatOutbox:ImmediatePublishEnabled=true` (Phase 1)
- `ChatOutbox:PollIntervalMs=2000`
- `ChatOutbox:BatchSize=50`
- `ChatOutbox:MaxAttempts=20`
- `ChatOutbox:MaxBackoffSeconds=60`
