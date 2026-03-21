# ProcessManager Bug Audit Report

**Date:** March 21, 2026
**Scope:** Full codebase review of ProcessManager.sln (.NET 8 / Blazor Server / ASP.NET Core API)

---

## Critical Severity

### 1. Query String Injection in ApiClient (50+ locations)
**File:** `src/ProcessManager.Web/Services/ApiClient.cs`
**Description:** Nearly every API method constructs query strings via raw string interpolation without URL-encoding user-supplied parameters. A user searching for `test&page=999999` could inject extra query parameters. Only one call site uses `Uri.EscapeDataString()`.
**Fix:** Use `Uri.EscapeDataString()` on all user-supplied query parameters, or switch to `QueryHelpers.AddQueryString()`.

### 2. Null Reference on Job Status Check
**File:** `src/ProcessManager.Api/Controllers/StepExecutionsController.cs` (~line 90)
**Description:** `se.Job.Status` is accessed without verifying `se.Job` is not null after `FirstOrDefaultAsync`. If the Job navigation property isn't loaded, this throws a `NullReferenceException`.
**Fix:** Add `if (se.Job is null) return BadRequest(...)` before the status check.

### 3. Null Reference on ProcessStep in StepExecutionsController
**File:** `src/ProcessManager.Api/Controllers/StepExecutionsController.cs` (~line 311)
**Description:** `se.ProcessStep.StepTemplateId` is dereferenced without a null check. If ProcessStep isn't loaded by the Include, this crashes.
**Fix:** Add null guard before accessing `se.ProcessStep`.

### 4. Missing Toast Service Injection in ChangePassword
**File:** `src/ProcessManager.Web/Components/Pages/Account/ChangePassword.razor` (~line 82)
**Description:** The component calls `Toast.ShowSuccess()` and `Toast.ShowError()` but never injects `ToastService`. This causes a compilation/runtime error.
**Fix:** Add `@inject ToastService Toast` at the top of the file.

---

## High Severity

### 5. Timing Attack in Swagger Auth Middleware
**File:** `src/ProcessManager.Api/Middleware/SwaggerBasicAuthMiddleware.cs` (line 46)
**Description:** Password comparison uses standard `==` operator, which is vulnerable to timing attacks that can reveal the password character by character.
**Fix:** Use `CryptographicOperations.FixedTimeEquals()` for constant-time comparison.

### 6. Null-Forgiving Operator on JWT Key
**File:** `src/ProcessManager.Api/Controllers/AuthController.cs` (~line 219)
**Description:** `jwtConfig["Key"]!` uses a forced null-forgiving operator. If the configuration key is missing, this throws a `NullReferenceException` with no actionable error message instead of a clear startup/configuration error.
**Fix:** Validate configuration: `var key = jwtConfig["Key"] ?? throw new InvalidOperationException("JWT Key not configured")`.

### 7. Weak Password Policy
**File:** `src/ProcessManager.Api/Program.cs` (lines 42-45)
**Description:** Password policy does not require uppercase letters or non-alphanumeric characters. Passwords like `password1` or `12345678` are accepted.
**Fix:** Set `RequireNonAlphanumeric = true` and `RequireUppercase = true`.

### 8. Timer Callback Accesses UI from Background Thread
**File:** `src/ProcessManager.Web/Components/Shared/ToastContainer.razor` (line 54)
**Description:** A Timer callback calls `ToastSvc.Dismiss(id)` and triggers a `StateHasChanged()` without wrapping it in `InvokeAsync()`. In Blazor Server, Timer callbacks run on a thread-pool thread, not the synchronization context.
**Fix:** Wrap in `await InvokeAsync(() => { ToastSvc.Dismiss(id); StateHasChanged(); })`.

### 9. Memory Leak in SearchBox Debounce Timer
**File:** `src/ProcessManager.Web/Components/Shared/SearchBox.razor` (lines 26-34)
**Description:** Each keystroke creates a new `Timer` object. Old timers are stopped but intermediate timers may not be fully disposed. Over a long session with heavy typing, this leaks resources.
**Fix:** Reuse a single `CancellationTokenSource` instead of creating new Timer objects, or properly dispose each timer before creating a new one.

### 10. Missing Error Handling in DocumentList WithdrawApproval
**File:** `src/ProcessManager.Web/Components/Pages/Documents/DocumentList.razor` (lines 445-456)
**Description:** The `WithdrawApproval` method makes API calls without try-catch. If the call fails, no error feedback is shown to the user and the exception propagates unhandled.
**Fix:** Wrap in try-catch and call `Toast.ShowError()`.

### 11. SSL Certificate Validation Disabled in Production
**File:** `src/ProcessManager.Api/Program.cs` (line 31)
**Description:** `Trust Server Certificate=true` is hardcoded in the connection string builder regardless of environment. This disables SSL certificate validation for PostgreSQL, allowing potential man-in-the-middle attacks in production.
**Fix:** Conditionally set based on environment: `Trust Server Certificate={isDevelopment}`.

---

## Medium Severity

### 12. Race Condition in DomainVocabulary Activation
**File:** `src/ProcessManager.Api/Controllers/DomainVocabulariesController.cs` (lines 118-122)
**Description:** Activating a vocabulary fetches all active ones, deactivates them, then saves. Two concurrent requests could both pass the read before either writes, resulting in multiple active vocabularies.
**Fix:** Use a database-level unique filtered index or wrap in a serializable transaction.

### 13. Missing [Required] on File Upload DTO
**File:** `src/ProcessManager.Api/DTOs/CommonDtos.cs` (line 33)
**Description:** `ImageUploadRequest.File` uses `null!` to suppress warnings but lacks a `[Required]` attribute. A null file could pass model binding and cause a `NullReferenceException` in the image storage service.
**Fix:** Add `[Required]` attribute to the `File` property.

### 14. Missing Connection Pool / Command Timeout Configuration
**File:** `src/ProcessManager.Api/Program.cs` (line 37)
**Description:** The DbContext is configured without explicit connection pool size or command timeout. Long-running queries can block connections indefinitely, and the default pool size (25) may be insufficient under load.
**Fix:** Add `npgOptions.CommandTimeout(30)` and `npgOptions.EnableRetryOnFailure(3)`.

### 15. Null Check Missing on Process Steps Collection
**File:** `src/ProcessManager.Api/Controllers/ProcessesController.cs` (lines 317-318)
**Description:** `process.ProcessSteps.FirstOrDefault()` is called without verifying the collection was loaded. If the `Include` fails silently, `FirstOrDefault` returns null and subsequent code assumes a non-null result.
**Fix:** Add `if (process.ProcessSteps is null || !process.ProcessSteps.Any()) return BadRequest(...)`.

### 16. Authorization Inconsistency on Competency Delete
**File:** `src/ProcessManager.Api/Controllers/CompetencyController.cs` (line 151)
**Description:** Delete requires `Admin` role only, but Create allows `Admin,Engineer`. Engineers can create records they cannot delete.
**Fix:** Align authorization policies or document the intentional difference.

### 17. Missing Null Check on Modal Data in ApprovalQueue
**File:** `src/ProcessManager.Web/Components/Pages/ApprovalQueue.razor` (lines 219-226)
**Description:** `_selected!` uses null-forgiving operator in `DoApprove()`. If `_selected` becomes null between modal open and submit (e.g., due to re-render), this throws.
**Fix:** Capture `_selected` in a local variable at the start of the method.

### 18. Stale State Risk in MyWork Navigation
**File:** `src/ProcessManager.Web/Components/Pages/Work/MyWork.razor` (lines 206-218)
**Description:** The `_starting` flag is not reliably reset if `Nav.NavigateTo()` fails silently. The component also lacks `IDisposable` and `CancellationToken` support, so in-flight API calls continue after the user navigates away.
**Fix:** Implement `IDisposable` with `CancellationTokenSource` and reset state in a finally block.

---

## Low Severity

### 19. Race Condition in DataSeeder Idempotency
**File:** `src/ProcessManager.Api/Data/DataSeeder.cs` (lines 15-294)
**Description:** The `Any()` check for existing data is not transactional. Two concurrent app instances starting up could both pass the check and attempt duplicate inserts.
**Fix:** Use database-level unique constraints and handle `DbUpdateException` gracefully.

### 20. Missing Input Validation in DocumentApprovalsController
**File:** `src/ProcessManager.Api/Controllers/DocumentApprovalsController.cs` (line 108)
**Description:** `dto.StepAssignments` dictionary is accessed without checking if the dictionary itself is null.
**Fix:** Add `if (dto.StepAssignments is null || dto.StepAssignments.Count == 0) return BadRequest(...)`.

### 21. Test Compilation Errors (StepTemplateTests)
**File:** `tests/ProcessManager.Tests/StepTemplateTests.cs` (lines 59-116)
**Description:** 14 compilation errors due to missing `DataType` parameter in `PortCreateDto` constructor calls. The DTO was updated but the tests were not. The test suite does not build.
**Fix:** Update all `PortCreateDto` constructor calls in the test file to include the `DataType` parameter.

---

## Summary

| Severity | Count |
|----------|-------|
| Critical | 4     |
| High     | 7     |
| Medium   | 7     |
| Low      | 3     |
| **Total** | **21** |

**Most urgent fixes:** The query string injection (#1), null references (#2, #3), and the missing service injection (#4) are the highest priority as they can cause runtime crashes or security vulnerabilities in production.
