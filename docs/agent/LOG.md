# Agent Maintenance Log

Newest entries at the bottom.

## 2026-09-26: Duplicate prompt responses in a single save
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `StepExecutionsController.SavePromptResponses` inserted duplicate `PromptResponse` rows when one request contained the same prompt more than once. The upsert now checks the change tracker first. Added 3 tests for the prompt-response endpoints, which had no coverage.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/21
- **Notes:** `CLAUDE.md`, `docs/agent/LOG.md` and `docs/agent/BACKLOG.md` didn't exist, so this run created the two docs/agent files. Without a CLAUDE.md, the task's default hard rules applied.
- **Follow-ups:** See BACKLOG.md → Agent-discovered.
