# Agent Maintenance Log

Newest entries at the bottom.

## 2026-09-25: Duplicate prompt responses within one save request
- **Task:** Bug fix (priority 2). `SavePromptResponses` inserted duplicate rows when a single request repeated a content block.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/20
- **Notes:** This entry was copied from that PR's branch. The PR's LOG/BACKLOG never reached `main`.

## 2026-09-26: Duplicate prompt responses in a single save
- **Task:** Bug fix (priority 2). Same bug as 2026-09-25. The earlier LOG wasn't on `main`, so this run didn't know about PR #20.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/21
- **Notes:** This entry was copied from that PR's branch. **#20 and #21 duplicate each other. Merge one and close the other.**

## 2026-09-27: Renaming a domain vocabulary to an existing name
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `PUT /api/domainvocabularies/{id}` didn't check for duplicate names, although `DomainVocabulary.Name` has a unique index and `POST` already returns 409. On Postgres the rename hit the unique index and returned a 500. On EF InMemory it silently saved a duplicate. Update now returns 409 Conflict, matching Create and `ItemsController.Update`. Added `DomainVocabularyTests.cs` with the reproduction plus characterization tests for create-conflict, same-name update, rename, and 404. 172/172 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/22
- **Notes:** The prior runs' LOG/BACKLOG weren't on `main`, so this branch carries both forward. To stop runs from repeating each other's work, it would help to merge the `docs/agent` files to `main` quickly, or to keep them on a long-lived branch.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.
