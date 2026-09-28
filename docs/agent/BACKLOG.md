# Backlog

Items marked **[ready]** may be picked up by the maintenance agent. Only the owner marks items ready.

## Owner

_(none yet)_

## Agent-discovered

- [ ] **`.Result` on async call in auth code.** `AuthController.cs:100` calls `_userManager.GetRolesAsync(u).Result` inside a projection. It's sync-over-async and can cause thread-pool starvation. Not touched because it's authentication code. (2026-09-26)
- [ ] **Negative paging parameters cause 500s on Postgres.** Every list endpoint (Jobs, Items, Batches, Kinds, Processes, StepTemplates, StepExecutions, Workflows, DomainVocabularies) computes `Skip((page - 1) * pageSize).Take(pageSize)` without validation. `page=0` or `pageSize<0` produces a negative OFFSET/LIMIT, which Npgsql rejects. EF InMemory silently accepts these values, so the tests don't catch it. The fix needs a decision: clamp the values or return 400. (2026-09-26)
- [ ] **`UpdateStep` accepts any Sequence.** `ProcessesController.UpdateStep` assigns `dto.Sequence` without validation, which can create duplicate or gapped sequences. `StepExecutionsController.Start` enforces ordering by looking up `Sequence - 1`, so on a gap the ordering check is silently skipped. `AddFlow` adjacency also depends on contiguous sequences. (2026-09-26)
- [ ] **Step transitions ignore Job status.** `StepExecutions` Complete/Skip/Fail, port transactions and execution data don't check that the parent Job is InProgress. Only Start does, so steps on an OnHold or Cancelled job can still be completed. Confirm whether that's intended. (2026-09-26)
- [ ] **No recovery path from a Failed step.** Once a StepExecution is `Failed` it has no allowed transitions, and `Jobs/{id}/complete` requires every step to be Completed or Skipped. A job with any failed step can only be cancelled. Is that the intended design? (2026-09-26)
- [ ] **No unique index for prompt responses.** `PromptResponse` has no unique index on (StepExecutionId, ProcessStepContentId, StepTemplateContentId), so concurrent saves could still create duplicates. Fixing it needs a migration. (2026-09-26)
- [ ] **N+1 queries in `SavePromptResponses`.** Each response item does one content lookup and one existing-row lookup. Preloading both sets per request would fix it. (2026-09-26)
- [ ] **Stray `build_output.txt` at the repo root.** It's committed and looks like leftover build output. Consider deleting it and adding it to .gitignore. (2026-09-26)
- [ ] **Missing `CLAUDE.md`.** The maintenance task expects one to define module boundaries and allowed areas. Adding it would make unattended runs less conservative. (2026-09-26)
- [ ] **Reports endpoints fail on non-positive query values.** `GET /api/reports/throughput?days=0` (or a negative value) calls `Enumerable.Range(0, days)` with a negative count and throws, which returns a 500. `recent-completions?count=-1` passes a negative `Take` to EF. The fix needs a decision: clamp the value or return 400. (2026-09-27)
- [ ] **Agent LOG/BACKLOG aren't on `main`.** Each maintenance run branches from `main`, so it can't see earlier runs' notes. That's why PRs #20 and #21 fix the same bug. Merge the `docs/agent` files, or tell the task to read them from open agent PRs. (2026-09-27)
- [ ] **Maintenance runs target a stale `main`.** `origin/main` is 214 commits behind `masterbranch-james` (PR #16). Agent fixes land on old code and may need to be ported. Decide which branch the daily task should branch from. (2026-09-28)
- [ ] **Deleting a Grade that a WorkflowLinkCondition uses isn't guarded.** `WorkflowLinkCondition.GradeId` is `Restrict`, and neither `DeleteGrade` nor `Delete` (Kind) checks it. Today a condition grade must match an output port's grade, so the Port check usually blocks the delete. If that port's grade is later changed, though, the delete would fail with an FK violation (500). (2026-09-28)
