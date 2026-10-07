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
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/24
- **Notes:** `masterbranch-james` has the same gap in `ProcessesController.Delete` and `DeleteStep`, so the fix needs porting there. Agent PRs #20 through #23 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-09-30: Assigning an Item to another Job's Batch on update
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `PUT /api/items/{id}` with a `BatchId` checked that the Batch's Kind matched but not its Job. An Item could be moved into a Batch that belongs to a different Job. `POST /api/items` and `POST /api/batches/{id}/items/{itemId}` both already reject this. Update now returns 400 with the same message as Create. Added a reproduction test and a same-Job characterization test to `ItemTests2.cs`. 169/169 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/25
- **Notes:** `masterbranch-james` has the same gap in `ItemsController.Update`, so the fix needs porting there. Agent PRs #20 through #24 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-01: Port update skipped quantity-rule validation
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `PUT /api/steptemplates/{id}/ports/{portId}` checked a Material port's Grade/Kind but not its quantity rule. `Exactly`/`ZeroOrN` with `QtyRuleN = 0`, or `Range` with Min > Max, was saved even though `POST /api/steptemplates` and `POST .../ports` reject the same values with 400. I moved the quantity-rule checks into a shared `ValidateQtyRule` helper. `ValidatePort` and `UpdatePort` both call it now, and `UpdatePort` returns 400 `{ errors }`, the same shape as `AddPort`. Added the first `UpdatePort` tests to `StepTemplateTests.cs`: 2 reproductions and 1 valid-update characterization. 170/170 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/26
- **Notes:** `masterbranch-james` has the same gap in `StepTemplatesController.UpdatePort`, so the fix needs porting there. Agent PRs #20 through #25 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-02: Duplicate condition grades when creating a workflow link
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `POST /api/workflows/{id}/links` added one `WorkflowLinkCondition` per entry in `ConditionGradeIds` and didn't check for repeats. `(WorkflowLinkId, GradeId)` has a unique index, so on Postgres a repeated grade returned a 500. On EF InMemory it saved duplicate conditions. `POST .../links/{linkId}/conditions` already rejects a duplicate with 409. Create now returns 400 when the list repeats a grade. Added a reproduction test to `WorkflowTests.cs`. 168/168 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/27
- **Notes:** I chose to reject with 400 rather than silently dedupe, because a repeated grade is most likely a client bug. Either choice removes the 500. `masterbranch-james` has the same gap in `WorkflowsController.CreateLink`, so the fix needs porting there. Agent PRs #20 through #26 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-03: Empty port transactions were accepted
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `docs/data-model.md` (PortTransaction constraints) requires that a transaction has an item, a batch, or a quantity > 0. `POST /api/step-executions/{id}/port-transactions` didn't check this, so a transaction with no item, no batch and `Quantity` 0 or negative was saved. That's a traceability record of nothing. It now returns 400 in that case. Added 2 reproductions (quantity 0 and -3) and 1 characterization test (an untracked transaction with quantity 5 still succeeds) to `StepExecutionTests.cs`. 170/170 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/28
- **Notes:** I enforced only the documented rule. A zero or negative quantity is still accepted when an item or batch is given, because the docs don't cover it. `masterbranch-james` probably has the same gap in `StepExecutionsController.AddPortTransaction`. Agent PRs #20 through #27 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-04: Port transactions accepted another Job's Item or Batch
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `POST /api/step-executions/{id}/port-transactions` checked an Item's or Batch's Kind against the port but not its Job. Another Job's Item or Batch could be recorded, and on an output port its Grade (and its Batch members' Grades) was changed. The endpoint now returns 400 when the Item's or Batch's `JobId` differs from the step execution's. Added 3 tests to `StepExecutionTests.cs` (input Item, output Item with Grade unchanged, input Batch). All 3 failed before the fix (201) and pass after it. 170/170 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/29
- **Notes:** This came from the 2026-10-03 Agent-discovered backlog item. `masterbranch-james` probably has the same gap. Agent PRs #20 through #28 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-05: Empty GradeName when adding an Item to a Batch
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `POST /api/batches/{id}/items/{itemId}` makes the Item inherit the Batch's Grade by setting `GradeId`. The new Grade wasn't loaded into the DbContext, so on save EF cleared the Item's `Grade` navigation and the response came back with the new `gradeId` but `gradeName: ""`. The data saved was correct. Only the response was wrong. The endpoint now loads the Batch's Grade and assigns it to the Item. Added `AddItem_WithDifferentGrade_ReturnsInheritedGradeName` to `BatchTests.cs`. It failed before the fix (`""` vs `"Good"`) and passes after. 168/168 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/30
- **Notes:** The in-repo UI (`BatchDetail.razor`) ignores this response and reloads, so the visible impact is on API consumers only. I picked an area that no open agent PR touches, so it won't conflict with them. `masterbranch-james` has the same code. Agent PRs #20 through #29 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-06: Empty JobName from a Job's Items and Batches lists
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `GET /api/jobs/{id}/items` and `GET /api/jobs/{id}/batches` didn't `Include` the Job, so every row came back with `jobName: ""`. All the other Item and Batch endpoints (`/api/items`, `/api/batches`, `/api/batches/{id}/items`) include it. Both queries now `Include` the Job. Added `GetItems_ReturnsJobsItemsWithJobName` and `GetBatches_ReturnsJobsBatchesWithJobName` to `JobTests.cs`, the first tests for these two endpoints. Both failed before the fix (`""` vs the Job's name) and pass after. 169/169 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/31
- **Notes:** The in-repo UI doesn't call these two endpoints, so the impact is on API consumers only. No open agent PR touches `JobsController`. `masterbranch-james` has the same two queries without the Include. Agent PRs #20 through #30 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-07: Reports counted cancelled jobs as completed
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `POST /api/jobs/{id}/cancel` sets `CompletedAt`, as `docs/data-model.md` documents ("completed or cancelled"). `GET /api/reports/throughput` counted every job with a `CompletedAt` in its "Completed" series, and `GET /api/reports/summary` averaged every job with both timestamps into `AvgJobDurationHours`. Cancelled jobs therefore showed up as completions on the Reports chart and skewed "Avg Job Duration". Both queries now also require `Status == Completed`, the same filter `CompletedThisMonth` and `recent-completions` already use. Added `ReportTests.cs`, the first tests for `ReportsController`: 2 reproductions (throughput and average duration with a cancelled job) and 1 characterization (a completed job is still counted). Both reproductions failed before the fix and pass after. 170/170 passing, 0 warnings.
- **PR:** _(see below)_
- **Notes:** The tests compare before/after values because the test class shares one database and seeded data, so they don't depend on absolute counts. No open agent PR touches `ReportsController`. `masterbranch-james` has the same two queries. Agent PRs #20 through #31 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.
