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
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/32
- **Notes:** The tests compare before/after values because the test class shares one database and seeded data, so they don't depend on absolute counts. No open agent PR touches `ReportsController`. `masterbranch-james` has the same two queries. Agent PRs #20 through #31 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-08: Blank serial number accepted when updating a serialized Item
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `docs/data-model.md` (Item constraints) says `serial_number` is required when the Item's Kind is serialized, and `POST /api/items` rejects a blank one with 400. `PUT /api/items/{id}` only checked uniqueness, so `""` or whitespace was saved on a serialized Item and its serial number was lost. Update now returns 400 with the same message as Create. Added to `ItemTests2.cs` a reproduction theory (`""` and `"   "`, which also checks that the serial is unchanged) and a characterization test (a valid rename still succeeds). Both reproduction cases failed before the fix (200) and pass after it. 170/170 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/33
- **Notes:** I only blocked blank serials for serialized Kinds. For non-serialized Kinds a blank serial is still saved as `""` rather than `null` (see backlog). PR #25 also edits `ItemsController.Update`, but in the `BatchId` block a few lines below, so the two shouldn't conflict. `masterbranch-james` has the same code. Agent PRs #20 through #32 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-09: Second flow on a reused StepTemplate's port was rejected
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** A StepTemplate can be placed at more than one ProcessStep in a Process. `POST /api/processes/{id}/flows` checked for duplicate connections by Port id across the whole Process, so once a template's port was connected at one step, the same port on a later step returned 409 and could never be connected. `GET /api/processes/{id}/validate` checks coverage per step, so it kept warning that the later port was unconnected, a warning the user couldn't clear. The duplicate check now matches on (ProcessStep, Port). That still enforces `docs/data-model.md` ("an output/input port may connect to at most one flow") for each step. Added `AddFlow_TemplateReusedAtTwoSteps_ConnectsEachStepsPorts` (reproduction: 409 before the fix, 201 and no validation warnings after) and `AddFlow_DuplicateTargetPort_ReturnsConflict` (characterization: the target-side check still blocks duplicates on the same step) to `ProcessTests.cs`. 169/169 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/34
- **Notes:** The `ProcessId` filter was dropped from the check because `AddFlow` already verifies both steps belong to the Process. PR #24 also edits `ProcessesController`, but in `Delete`/`DeleteStep`, so the two shouldn't conflict. `masterbranch-james` has the same check. Agent PRs #20 through #33 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-10: Negative quantity accepted when updating a Batch
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `CreateBatchDto.Quantity` has `[Range(0, int.MaxValue)]`, so `POST /api/batches` rejects a negative quantity. `UpdateBatchDto.Quantity` had no attribute, so `PUT /api/batches/{id}` saved a negative one. I added the same `[Range]` to the update DTO, and model validation now returns 400. Added `Update_NegativeQuantity_ReturnsBadRequestAndKeepsQuantity` (reproduction: 200 before the fix, 400 and the quantity unchanged after it) and the characterization theory `Update_NonNegativeQuantity_Succeeds` (0 and 75) to `BatchTests.cs`. 170/170 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/35
- **Notes:** `BatchDetail.razor` already has `min="0"` on the input, but a typed negative value still reached the API. No open agent PR touches `UpdateBatchDto` or `BatchesController.Update`. `masterbranch-james` has the same DTO. Agent PRs #20 through #34 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-11: Over-length serial number accepted when updating an Item
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** `CreateItemDto.SerialNumber` has `[StringLength(100)]`, matching the column's `HasMaxLength(100)`. `UpdateItemDto.SerialNumber` had no limit, so `PUT /api/items/{id}` passed a longer serial through to the database. On Postgres that fails on save with a 500. EF InMemory doesn't enforce max length, so it saved. I added the same `[StringLength(100)]` to the update DTO, and model validation now returns 400. Added `Update_SerialNumberOver100Chars_ReturnsBadRequestAndKeepsSerial` (reproduction: 200 before the fix, 400 with the serial unchanged after it) and `Update_SerialNumberOf100Chars_Succeeds` (characterization at the limit) to `ItemTests2.cs`. 169/169 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/36
- **Notes:** This came from the 2026-10-10 Agent-discovered backlog item. It was deferred because PRs #25 and #33 edit `ItemsController.Update`. This fix only touches the DTO, and I put the tests mid-file rather than at the end, to avoid conflicts with those PRs' test additions. `masterbranch-james` has the same DTO. Agent PRs #20 through #35 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-12: Numeric PromptType strings saved undefined prompt types
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests.
- **What:** Both `AddPromptBlock` endpoints (`POST /api/steptemplates/{id}/content/prompt` and `POST /api/processes/{id}/steps/{stepId}/content/prompt`) validated `PromptType` with `Enum.TryParse`. `Enum.TryParse` accepts any integer string, so `"99"` or `"-1"` was saved as an undefined prompt type instead of returning the endpoint's own 400 "Unknown PromptType". Both endpoints now also require `Enum.IsDefined`. Added `PromptBlockTests.cs`, the first tests for these endpoints: reproduction theories for both (`"99"`, `"-1"`, `"NotAType"`, asserting 400 and that nothing was saved) and characterization tests for named types. The numeric cases failed before the fix (201) and pass after it. 176/176 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/37
- **Notes:** A numeric string for a defined value (for example `"1"`) is still accepted, so the behavior change is as small as possible. I put the tests in a new file to avoid conflicts with the open agent PRs that append to `ProcessTests.cs`/`StepTemplateTests.cs`. `masterbranch-james` probably has the same checks. Agent PRs #20 through #36 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-13: Workflow Kind-mismatch warning suppressed by non-Material ports
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests. No BACKLOG items are marked [ready].
- **What:** Rule 15 in `POST /api/workflows/{id}/validate` compared the `KindId` sets of the source's last-step outputs and the target's first-step inputs. Parameter, Characteristic and Condition ports have a null `KindId`, so when both sides had one, `null` matched `null` and the "source output Kinds do not match target input Kinds" warning was suppressed even though the Material Kinds differed. Only ports with a Kind are now compared. If neither side has Material ports, there's still no warning. Added `WorkflowValidationTests.cs` with 1 reproduction (failed before the fix, passes after) and 2 characterization tests (a plain mismatch still warns, and matching Kinds with extra Parameter ports don't). 170/170 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/38
- **Notes:** I put the tests in a new file because PR #27 appends to `WorkflowTests.cs`. No open agent PR touches `WorkflowsController.Validate`. `masterbranch-james` moved this check into `WorkflowStructuralValidator.cs` and has the same null-Kind bug. Agent PRs #20 through #37 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-18: Process validation warned about non-Material ports that can't have flows
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests. No BACKLOG items are marked [ready]. There are no log entries for 2026-10-14 to 2026-10-17. The task either didn't run or didn't record anything.
- **What:** `GET /api/processes/{id}/validate` (Rule 4 flow coverage) warned about every unconnected port between adjacent steps, including Parameter, Characteristic and Condition ports. `AddFlow` rejects non-Material ports (Rule 3), so these warnings could never be cleared. The coverage check now only considers Material ports. Added `ProcessValidationTests.cs` with 2 tests: one where non-Material ports alone produce no warnings (3 warnings before the fix), and one where only the unconnected Material ports warn (5 before the fix, 2 after). Both failed before the fix and pass after it. 169/169 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/39
- **Notes:** I put the tests in a new file because PRs #24 and #34 append to `ProcessTests.cs`. Neither touches `Validate`. `masterbranch-james` has the same query. Agent PRs #20 through #38 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-19: Blank serial number stored as "" when creating an Item
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests. No BACKLOG items are marked [ready].
- **What:** `POST /api/items` saved `SerialNumber` exactly as sent. For a non-serialized Kind, `""` or whitespace was stored as-is instead of `null`. `Item` has a unique index on `(KindId, SerialNumber)` with no filter. Postgres treats NULLs as distinct but not `""`, so a second blank-serial Item of the same Kind would fail on save with a 500. The in-repo UI (`ItemList.razor`) already converts a blank serial to `null` before calling the API, so only API consumers hit this. Create now stores a blank or whitespace serial as `null`. Added `ItemSerialNumberTests.cs` with a reproduction theory (`""` and `"   "`, two Items each, both expected `null`) and a characterization test (a non-blank serial on a non-serialized Kind is kept). Both reproduction cases failed before the fix (`""` / `"   "` returned) and pass after it. 170/170 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/40
- **Notes:** I only normalize blank serials to `null`. Non-blank serials aren't trimmed, even though the UI trims them, because that would change stored values for existing clients. EF InMemory doesn't enforce unique indexes, so the test asserts the stored value rather than the 500. `PUT /api/items/{id}` has the same `""` behavior (see the 2026-10-08 backlog item). I left it alone because PRs #25 and #33 both edit `ItemsController.Update`. I put the tests in a new file because PRs #25, #33 and #36 add to `ItemTests2.cs`. `masterbranch-james` has the same `SerialNumber = dto.SerialNumber` in Create (line 118) and the same unfiltered index. Agent PRs #20 through #39 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-20: Deleting a Kind or Grade that Items or Batches still reference
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests. No BACKLOG items are marked [ready].
- **What:** `DELETE /api/kinds/{id}` and `DELETE /api/kinds/{kindId}/grades/{gradeId}` only checked Ports. `Item.KindId`, `Item.GradeId`, `Batch.KindId` and `Batch.GradeId` are all `Restrict`, and an Item or Batch can use a Kind that no Port references. On Postgres these deletes returned a 500. On EF InMemory they succeeded and left Items and Batches pointing at a missing Kind or Grade. Both endpoints now return 409 Conflict when an Item or Batch references the Kind or Grade, the same as the existing Port guard. Added `KindDeleteReferenceTests.cs` with 4 reproductions (Kind with an Item, Kind with a Batch, Grade with an Item, Grade with a Batch) and 1 characterization test (an unreferenced Grade and Kind still delete with 204). All 4 reproductions failed before the fix (204) and pass after it. 172/172 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/41
- **Notes:** Item/Batch Create already require the Grade to belong to the Kind, so the Kind-level Item/Batch check also covers the Grades that a Kind delete cascades to. I put the tests in a new file so they don't conflict with open agent PRs. No open agent PR touches `KindsController`. `masterbranch-james` has the same gap (it checks Ports and BomLines only). Agent PRs #20 through #40 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-21: Job Items list returned no Batch code
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests. No BACKLOG items are marked [ready].
- **What:** `GET /api/jobs/{id}/items` loaded each Item's Kind and Grade but not its Batch, so `BatchCode` was always `null` for an Item in a Batch, even though `BatchId` was set. `GET /api/items`, `GET /api/items/{id}` and `GET /api/batches/{id}/items` all include `Batch`. The query now includes `Batch` too. Added `JobItemsBatchCodeTests.cs` with 1 reproduction (an Item in a Batch reports the Batch's code; it returned `null` before the fix) and 1 characterization test (an Item without a Batch still reports `null`). 169/169 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/42
- **Notes:** PR #31 adds `.Include(i => i.Job)` to the same query. I put the new Include first, not next to #31's line, and checked with a three-way merge that the two PRs merge cleanly. The in-repo UI doesn't call this endpoint, so only API consumers see the bug. I put the tests in a new file so they don't conflict with open agent PRs. `masterbranch-james` has the same query without `Batch` (`JobsController.cs` around line 451). Agent PRs #20 through #41 are still open. #41 (2026-10-20) fixes the same bug as #23 (2026-09-28), so one of them can be closed.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-22: Prompt responses accepted content from other steps or unknown ids
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests. No BACKLOG items are marked [ready].
- **What:** `POST /api/step-executions/{id}/prompt-responses` saved responses without checking the referenced content block. An unknown `ProcessStepContentId`/`StepTemplateContentId` was saved with a dangling FK. On Postgres that's a 500 on save, and it can happen through the UI if a block is deleted in ProcessBuilder while an operator has the step open. A block from another ProcessStep or StepTemplate was also accepted, and its range limits were used for `IsOutOfRange`. The endpoint now returns 400 unless the block is on the execution's own ProcessStep or on that step's StepTemplate. Added `PromptResponseContentOwnershipTests.cs` with 4 reproductions (all returned 204 before the fix) and 2 characterization tests (a step's own process and template prompts still save). 173/173 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/43
- **Notes:** PRs #20 and #21 change the upsert lookup in the same method. I left those lines alone. Three-way merges with #20, #21 and the 2026-10-03 and 2026-10-04 branches are clean, and #20's `PromptResponseTests` pass with this fix merged in (183/183). I put the tests in a new file because #20 adds `PromptResponseTests.cs`. `masterbranch-james` has the same gap in both `prompt-responses` and `prompt-responses/batch`. Agent PRs #20 through #42 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-23: Deleting a Port that has port transactions
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests. No BACKLOG items are marked [ready].
- **What:** `DELETE /api/steptemplates/{id}/ports/{portId}` only checked Flows. `PortTransaction.Port` is `Restrict`, and a first-step input or last-step output port never has a Flow but does get transactions when a Job runs. On Postgres the delete returned a 500. On EF InMemory it succeeded and left transactions pointing at a missing Port. The endpoint now returns 409 Conflict when a PortTransaction references the Port, the same as the Flow guard. Added `PortDeleteReferenceTests.cs` with 1 reproduction (a first-step input port with a transaction; 204 before the fix, 409 and the port kept after it) and 1 characterization test (an unreferenced last-step output port still deletes with 204). 169/169 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/44
- **Notes:** PRs #26 (2026-10-01) and #37 (2026-10-12) also edit `StepTemplatesController`, in `UpdatePort`/`ValidatePorts` and `AddPromptBlock`. `git merge-tree` shows the source file merges cleanly with both; only `docs/agent` conflicts, as with every agent PR. I put the tests in a new file so they don't conflict with open agent PRs. `masterbranch-james` has the same gap (`StepTemplatesController.DeletePort` around line 327 checks Flows only). Agent PRs #20 through #43 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.

## 2026-10-24: Web search text wasn't URL-encoded
- **Task:** Bug fix (priority 2). Main was green: 0 build warnings, 167/167 tests. No BACKLOG items are marked [ready].
- **What:** `ProcessManager.Web/Services/ApiClient.cs` interpolated the user's search text (and `status`) into list URLs without encoding. A `&` split the value (`Nuts & Bolts` → `search=Nuts `), a `#` made the rest of the URL a fragment, and a `+` arrived as a space (`C++` matched nothing). This affected the search box on all 8 list pages. Both parameters now go through an `E()` helper (`Uri.EscapeDataString`), the same one `masterbranch-james` has. Added `ApiClientSearchEncodingTests.cs`, which drives the real `ApiClient` against the in-memory API: a 3-case reproduction theory (all 3 failed before the fix, pass after it) and 2 characterization tests (a plain search, and a `null` search that stays unfiltered). 172/172 passing, 0 warnings.
- **PR:** https://github.com/SequencerPro/Process_Manager/pull/45
- **Notes:** I first tried the `DomainVocabulariesController.Update` duplicate-name check, but PR #21 (2026-09-27) already fixes it. This run checked every open agent branch's changed files before choosing a fix. For the test, the test project now references `ProcessManager.Web` with `Aliases="Web"`, because Web and Api both define a top-level `Program`. I flagged this in the PR in case the owner prefers a separate Web test project. No open agent PR touches `ApiClient.cs`. Agent PRs #20 through #44 are still open.
- **Follow-ups:** Added 2 items to BACKLOG.md under Agent-discovered.
