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

## 2026-09-28: Deleting a Kind or Grade that Items or Batches still reference
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `DELETE /api/kinds/{id}` and `DELETE /api/kinds/{kindId}/grades/{gradeId}` checked only Ports for references. Items and Batches also reference Kind and Grade with `DeleteBehavior.Restrict`. On Postgres the delete hit an FK violation and returned a 500. On EF InMemory it succeeded and left orphaned Items and Batches. Both endpoints now return 409 Conflict, matching the existing Port check. Added 4 reproduction tests to `KindTests.cs` and `GradeTests.cs`. 171/171 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/23
- **Notes:** `origin/main` is 214 commits behind `masterbranch-james` (open PR #16), and that branch has the same bug in `KindsController`. The fix needs porting there, or these agent runs should target that branch. Prior agent PRs #20, #21 and #22 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-09-29: Deleting a Process or ProcessStep that Jobs or Workflows still reference
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `DELETE /api/processes/{id}` didn't check Jobs or WorkflowProcesses. `DELETE /api/processes/{id}/steps/{stepId}` didn't check StepExecutions. All three FKs are `Restrict`. On Postgres these deletes returned a 500. On EF InMemory they succeeded and left orphaned rows. Because creating a Job auto-creates a StepExecution per ProcessStep, a step became undeletable as soon as any Job used its Process. Both endpoints now return 409 Conflict, the same as the existing StepTemplate and WorkflowProcess guards. Added 3 reproduction tests to `ProcessTests.cs`. 170/170 passing, 0 warnings.
- **PR:** _(see below)_
- **Notes:** `masterbranch-james` has the same gap in `ProcessesController.Delete` and `DeleteStep`, so the fix needs porting there. Agent PRs #20 through #23 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.
