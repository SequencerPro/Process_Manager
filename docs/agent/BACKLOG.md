# Agent Backlog

Items marked `[ready]` may be picked up by the daily maintenance agent. Only the owner marks items `[ready]`.

## Ready

_(none yet)_

## Agent-discovered

- [ ] **Blocking `.Result` in AuthController** — `src/ProcessManager.Api/Controllers/AuthController.cs:100` calls `_userManager.GetRolesAsync(u).Result` inside a projection. Should be awaited. Not touched by the agent because it is authentication code. (2026-09-25)
- [ ] **Negative/zero pagination params cause 500 on Postgres** — every list endpoint does `Skip((page - 1) * pageSize).Take(pageSize)` with no validation. `page=0` or `pageSize<=0` yields a negative OFFSET/LIMIT, which PostgreSQL rejects. The InMemory test provider hides this. Consider clamping or returning 400. (2026-09-25)
- [ ] **SavePromptResponses accepts unknown/foreign content IDs** — `POST /api/step-executions/{id}/prompt-responses` doesn't verify the referenced `ProcessStepContentId`/`StepTemplateContentId` exists or belongs to the execution's step. A missing ID will hit an FK violation (500) on Postgres; a block from another step is silently accepted. (2026-09-25)
- [ ] **SavePromptResponses does 2 queries per response item** — `FindAsync` + `FirstOrDefaultAsync` per item (N+1). Could preload contents and existing responses in two queries. (2026-09-25)
- [ ] **Complete/Skip/Fail step don't check job status** — `StepExecutionsController.Start` requires the Job to be InProgress, but `Complete`, `Skip`, and `Fail` don't, so steps on a Cancelled/Completed job can still transition. Confirm intended behaviour before changing. (2026-09-25)
- [ ] **No CLAUDE.md** — the maintenance task expects a CLAUDE.md describing what the agent may touch; none exists. The agent treated migrations, auth, deployment config (`render.yaml`) and public API shapes as off-limits by default. (2026-09-25)
- [ ] **`build_output.txt` is committed at repo root** — looks like a stray build log; consider removing and adding to .gitignore. (2026-09-25)
