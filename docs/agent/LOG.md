# Agent Log

## 2026-09-25 — fix: duplicate prompt responses within one save request

- **Task:** Bug (priority 2). `SavePromptResponses` upserts by (StepExecution, content block) but only looked in the database, so the same block sent twice in one request inserted two rows.
- **Fix:** Check `PromptResponses.Local` (change tracker) before querying the DB.
- **Tests:** New `PromptResponseTests.cs` — reproduction test plus characterization tests for the previously uncovered prompt-response endpoints (create, out-of-range flag, cross-request upsert, validation, 404). 177/177 passing, 0 build warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/20
- **Follow-ups:** CLAUDE.md, LOG.md and BACKLOG.md did not exist; created LOG/BACKLOG. Added 6 agent-discovered items to BACKLOG.md.
