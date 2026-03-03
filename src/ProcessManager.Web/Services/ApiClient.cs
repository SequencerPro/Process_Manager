using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Web.Services;

/// <summary>
/// Typed HTTP client for the Process Manager API.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    // Auth header is injected per-request by TokenHandler — no manual
    // header management needed here.
    public ApiClient(HttpClient http, JsonSerializerOptions json)
    {
        _http = http;
        _json = json;
    }

    // ═══════════════════ Kinds ═══════════════════

    public Task<PaginatedResponse<KindResponseDto>?> GetKindsAsync(string? search = null, int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<KindResponseDto>>(
            $"api/kinds?search={search}&page={page}&pageSize={pageSize}", _json);

    public Task<KindResponseDto?> GetKindAsync(Guid id)
        => _http.GetFromJsonAsync<KindResponseDto>($"api/kinds/{id}", _json);

    public async Task<KindResponseDto?> CreateKindAsync(KindCreateDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/kinds", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<KindResponseDto>(_json);
    }

    public async Task<KindResponseDto?> UpdateKindAsync(Guid id, KindUpdateDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/kinds/{id}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<KindResponseDto>(_json);
    }

    public async Task DeleteKindAsync(Guid id)
    {
        var resp = await _http.DeleteAsync($"api/kinds/{id}");
        resp.EnsureSuccessStatusCode();
    }

    // ═══════════════════ Grades ═══════════════════

    public async Task<GradeResponseDto?> CreateGradeAsync(Guid kindId, GradeCreateDto dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/kinds/{kindId}/grades", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<GradeResponseDto>(_json);
    }

    public async Task<GradeResponseDto?> UpdateGradeAsync(Guid kindId, Guid gradeId, GradeUpdateDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/kinds/{kindId}/grades/{gradeId}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<GradeResponseDto>(_json);
    }

    public async Task DeleteGradeAsync(Guid kindId, Guid gradeId)
    {
        var resp = await _http.DeleteAsync($"api/kinds/{kindId}/grades/{gradeId}");
        resp.EnsureSuccessStatusCode();
    }

    // ═══════════════════ Step Templates ═══════════════════

    public Task<PaginatedResponse<StepTemplateResponseDto>?> GetStepTemplatesAsync(
        string? search = null, bool? active = null, string? status = null, int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<StepTemplateResponseDto>>(
            $"api/steptemplates?search={search}&active={active}&status={status}&page={page}&pageSize={pageSize}", _json);

    public Task<StepTemplateResponseDto?> GetStepTemplateAsync(Guid id)
        => _http.GetFromJsonAsync<StepTemplateResponseDto>($"api/steptemplates/{id}", _json);

    public async Task<StepTemplateResponseDto?> CreateStepTemplateAsync(StepTemplateCreateDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/steptemplates", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<StepTemplateResponseDto>(_json);
    }

    public async Task<StepTemplateResponseDto?> UpdateStepTemplateAsync(Guid id, StepTemplateUpdateDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/steptemplates/{id}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<StepTemplateResponseDto>(_json);
    }

    public async Task DeleteStepTemplateAsync(Guid id)
    {
        var resp = await _http.DeleteAsync($"api/steptemplates/{id}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<StepTemplateImageResponseDto?> UploadStepTemplateImageAsync(Guid stepTemplateId, IBrowserFile file)
    {
        // Buffer into MemoryStream so the body is seekable (required when API redirects HTTP→HTTPS)
        using var ms = new MemoryStream();
        await file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024).CopyToAsync(ms);
        ms.Position = 0;

        using var content = new MultipartFormDataContent();
        var sc = new StreamContent(ms);
        sc.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(sc, "file", file.Name);

        var resp = await _http.PostAsync($"api/steptemplates/{stepTemplateId}/images", content);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<StepTemplateImageResponseDto>(_json);
    }

    public async Task DeleteStepTemplateImageAsync(Guid stepTemplateId, Guid imageId)
    {
        var resp = await _http.DeleteAsync($"api/steptemplates/{stepTemplateId}/images/{imageId}");
        resp.EnsureSuccessStatusCode();
    }

    // ─── StepTemplate content blocks ───

    public async Task<List<StepTemplateContentResponseDto>> GetStepTemplateContentAsync(Guid stepTemplateId)
    {
        return await _http.GetFromJsonAsync<List<StepTemplateContentResponseDto>>(
            $"api/steptemplates/{stepTemplateId}/content", _json) ?? new();
    }

    public async Task<StepTemplateContentResponseDto?> AddStepTemplateTextBlockAsync(
        Guid stepTemplateId, AddStepTemplateTextBlockDto dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/steptemplates/{stepTemplateId}/content/text", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<StepTemplateContentResponseDto>(_json);
    }

    public async Task<StepTemplateContentResponseDto?> UploadStepTemplateContentImageAsync(
        Guid stepTemplateId, IBrowserFile file)
    {
        using var ms = new MemoryStream();
        await file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024).CopyToAsync(ms);
        ms.Position = 0;

        using var content = new MultipartFormDataContent();
        var sc = new StreamContent(ms);
        sc.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(sc, "file", file.Name);

        var resp = await _http.PostAsync($"api/steptemplates/{stepTemplateId}/content/image", content);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<StepTemplateContentResponseDto>(_json);
    }

    public async Task<StepTemplateContentResponseDto?> UpdateStepTemplateTextBlockAsync(
        Guid stepTemplateId, Guid contentId, UpdateStepTemplateTextBlockDto dto)
    {
        var resp = await _http.PutAsJsonAsync(
            $"api/steptemplates/{stepTemplateId}/content/{contentId}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<StepTemplateContentResponseDto>(_json);
    }

    public async Task ReorderStepTemplateContentAsync(
        Guid stepTemplateId, ReorderStepTemplateContentBlocksDto dto)
    {
        var resp = await _http.PutAsJsonAsync(
            $"api/steptemplates/{stepTemplateId}/content/reorder", dto, _json);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteStepTemplateContentBlockAsync(Guid stepTemplateId, Guid contentId)
    {
        var resp = await _http.DeleteAsync($"api/steptemplates/{stepTemplateId}/content/{contentId}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<StepTemplateContentResponseDto?> AddStepTemplatePromptBlockAsync(
        Guid stepTemplateId, AddStepTemplatePromptBlockDto dto)
    {
        var resp = await _http.PostAsJsonAsync(
            $"api/steptemplates/{stepTemplateId}/content/prompt", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<StepTemplateContentResponseDto>(_json);
    }

    public async Task<StepTemplateContentResponseDto?> UpdateStepTemplatePromptBlockAsync(
        Guid stepTemplateId, Guid contentId, UpdateStepTemplatePromptBlockDto dto)
    {
        var resp = await _http.PutAsJsonAsync(
            $"api/steptemplates/{stepTemplateId}/content/{contentId}/prompt", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<StepTemplateContentResponseDto>(_json);
    }

    public async Task<StepTemplateContentResponseDto?> PatchContentCategoryAsync(
        Guid stepTemplateId, Guid contentId, PatchContentCategoryDto dto)
    {
        var resp = await _http.PatchAsJsonAsync(
            $"api/steptemplates/{stepTemplateId}/content/{contentId}/category", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<StepTemplateContentResponseDto>(_json);
    }

    public async Task<MaturityReportDto?> GetStepTemplateMaturityAsync(Guid stepTemplateId)
        => await _http.GetFromJsonAsync<MaturityReportDto>(
            $"api/steptemplates/{stepTemplateId}/maturity", _json);

    /// <summary>Returns the absolute URL for an image relative path (e.g., "uploads/steptemplates/abc.jpg").</summary>
    public string GetImageUrl(string relativePath)
        => $"{_http.BaseAddress?.ToString().TrimEnd('/')}/{relativePath.TrimStart('/')}";

    // ─── Run Chart Widgets ───

    public async Task<List<RunChartWidgetResponseDto>> GetRunChartWidgetsAsync(Guid stepTemplateId)
        => await _http.GetFromJsonAsync<List<RunChartWidgetResponseDto>>(
            $"api/steptemplates/{stepTemplateId}/runcharts", _json) ?? new();

    public async Task<RunChartWidgetResponseDto?> CreateRunChartWidgetAsync(
        Guid stepTemplateId, RunChartWidgetCreateDto dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/steptemplates/{stepTemplateId}/runcharts", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<RunChartWidgetResponseDto>(_json);
    }

    public async Task<RunChartWidgetResponseDto?> UpdateRunChartWidgetAsync(
        Guid stepTemplateId, Guid widgetId, RunChartWidgetUpdateDto dto)
    {
        var resp = await _http.PutAsJsonAsync(
            $"api/steptemplates/{stepTemplateId}/runcharts/{widgetId}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<RunChartWidgetResponseDto>(_json);
    }

    public async Task DeleteRunChartWidgetAsync(Guid stepTemplateId, Guid widgetId)
    {
        var resp = await _http.DeleteAsync($"api/steptemplates/{stepTemplateId}/runcharts/{widgetId}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<PromptHistoryPointDto>> GetPromptHistoryAsync(
        Guid stepTemplateId, Guid contentId, int limit = 30)
        => await _http.GetFromJsonAsync<List<PromptHistoryPointDto>>(
            $"api/steptemplates/{stepTemplateId}/content/{contentId}/prompt-history?limit={limit}", _json)
           ?? new();

    // ═══════════════════ Analytics ═══════════════════

    public async Task<AnalyticsQueryResultDto?> RunAnalyticsQueryAsync(AnalyticsQueryDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/analytics/query", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<AnalyticsQueryResultDto>(_json);
    }

    // ═══════════════════ Ports ═══════════════════

    public async Task<PortResponseDto?> CreatePortAsync(Guid stepTemplateId, PortCreateDto dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/steptemplates/{stepTemplateId}/ports", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<PortResponseDto>(_json);
    }

    public async Task<PortResponseDto?> UpdatePortAsync(Guid stepTemplateId, Guid portId, PortUpdateDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/steptemplates/{stepTemplateId}/ports/{portId}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<PortResponseDto>(_json);
    }

    public async Task DeletePortAsync(Guid stepTemplateId, Guid portId)
    {
        var resp = await _http.DeleteAsync($"api/steptemplates/{stepTemplateId}/ports/{portId}");
        resp.EnsureSuccessStatusCode();
    }

    // ═══════════════════ Processes ═══════════════════

    public Task<PaginatedResponse<ProcessSummaryResponseDto>?> GetProcessesAsync(
        string? search = null, bool? active = null, string? status = null, int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<ProcessSummaryResponseDto>>(
            $"api/processes?search={search}&active={active}&status={status}&page={page}&pageSize={pageSize}", _json);

    public Task<ProcessResponseDto?> GetProcessAsync(Guid id)
        => _http.GetFromJsonAsync<ProcessResponseDto>($"api/processes/{id}", _json);

    public async Task<ProcessResponseDto?> CreateProcessAsync(ProcessCreateDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/processes", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ProcessResponseDto>(_json);
    }

    public async Task<ProcessResponseDto?> UpdateProcessAsync(Guid id, ProcessUpdateDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/processes/{id}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ProcessResponseDto>(_json);
    }

    public async Task DeleteProcessAsync(Guid id)
    {
        var resp = await _http.DeleteAsync($"api/processes/{id}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<ProcessStepResponseDto?> AddProcessStepAsync(Guid processId, ProcessStepCreateDto dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/processes/{processId}/steps", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ProcessStepResponseDto>(_json);
    }

    public async Task<ProcessStepResponseDto?> UpdateProcessStepAsync(Guid processId, Guid stepId, ProcessStepUpdateDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/processes/{processId}/steps/{stepId}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ProcessStepResponseDto>(_json);
    }

    public async Task DeleteProcessStepAsync(Guid processId, Guid stepId)
    {
        var resp = await _http.DeleteAsync($"api/processes/{processId}/steps/{stepId}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<FlowResponseDto?> AddFlowAsync(Guid processId, FlowCreateDto dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/processes/{processId}/flows", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<FlowResponseDto>(_json);
    }

    public async Task DeleteFlowAsync(Guid processId, Guid flowId)
    {
        var resp = await _http.DeleteAsync($"api/processes/{processId}/flows/{flowId}");
        resp.EnsureSuccessStatusCode();
    }

    public Task<ProcessValidationResultDto?> ValidateProcessAsync(Guid processId)
        => _http.GetFromJsonAsync<ProcessValidationResultDto>($"api/processes/{processId}/validate", _json);

    // ── Step Content ──

    public Task<List<ProcessStepContentResponseDto>?> GetStepContentAsync(Guid processId, Guid stepId)
        => _http.GetFromJsonAsync<List<ProcessStepContentResponseDto>>(
            $"api/processes/{processId}/steps/{stepId}/content", _json);

    public async Task<ProcessStepContentResponseDto?> AddTextBlockAsync(
        Guid processId, Guid stepId, AddTextBlockDto dto)
    {
        var resp = await _http.PostAsJsonAsync(
            $"api/processes/{processId}/steps/{stepId}/content/text", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(_json);
    }

    public async Task<ProcessStepContentResponseDto?> UploadStepContentImageAsync(
        Guid processId, Guid stepId, IBrowserFile file)
    {
        using var ms = new MemoryStream();
        await file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024).CopyToAsync(ms);
        ms.Position = 0;

        using var content = new MultipartFormDataContent();
        var sc = new StreamContent(ms);
        sc.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(sc, "file", file.Name);

        var resp = await _http.PostAsync(
            $"api/processes/{processId}/steps/{stepId}/content/image", content);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(_json);
    }

    public async Task<ProcessStepContentResponseDto?> UpdateTextBlockAsync(
        Guid processId, Guid stepId, Guid contentId, UpdateTextBlockDto dto)
    {
        var resp = await _http.PutAsJsonAsync(
            $"api/processes/{processId}/steps/{stepId}/content/{contentId}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(_json);
    }

    public async Task ReorderStepContentAsync(
        Guid processId, Guid stepId, ReorderContentBlocksDto dto)
    {
        var resp = await _http.PutAsJsonAsync(
            $"api/processes/{processId}/steps/{stepId}/content/reorder", dto, _json);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteStepContentBlockAsync(Guid processId, Guid stepId, Guid contentId)
    {
        var resp = await _http.DeleteAsync(
            $"api/processes/{processId}/steps/{stepId}/content/{contentId}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<ProcessStepContentResponseDto?> AddPromptBlockAsync(
        Guid processId, Guid stepId, AddPromptBlockDto dto)
    {
        var resp = await _http.PostAsJsonAsync(
            $"api/processes/{processId}/steps/{stepId}/content/prompt", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(_json);
    }

    public async Task<ProcessStepContentResponseDto?> UpdatePromptBlockAsync(
        Guid processId, Guid stepId, Guid contentId, UpdatePromptBlockDto dto)
    {
        var resp = await _http.PutAsJsonAsync(
            $"api/processes/{processId}/steps/{stepId}/content/{contentId}/prompt", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(_json);
    }

    public async Task<List<PromptResponseDto>> GetPromptResponsesAsync(Guid stepExecutionId)
    {
        return await _http.GetFromJsonAsync<List<PromptResponseDto>>(
            $"api/step-executions/{stepExecutionId}/prompt-responses", _json) ?? new();
    }

    public async Task SavePromptResponsesAsync(Guid stepExecutionId, SavePromptResponsesDto dto)
    {
        var resp = await _http.PostAsJsonAsync(
            $"api/step-executions/{stepExecutionId}/prompt-responses", dto, _json);
        resp.EnsureSuccessStatusCode();
    }

    // ═══════════════════ Jobs ═══════════════════

    public Task<PaginatedResponse<JobResponseDto>?> GetJobsAsync(
        string? search = null, string? status = null, Guid? processId = null,
        int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<JobResponseDto>>(
            $"api/jobs?search={search}&status={status}&processId={processId}&page={page}&pageSize={pageSize}", _json);

    public Task<JobResponseDto?> GetJobAsync(Guid id)
        => _http.GetFromJsonAsync<JobResponseDto>($"api/jobs/{id}", _json);

    public async Task<JobResponseDto?> CreateJobAsync(CreateJobDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/jobs", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JobResponseDto>(_json);
    }

    public async Task<JobResponseDto?> UpdateJobAsync(Guid id, UpdateJobDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/jobs/{id}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JobResponseDto>(_json);
    }

    public async Task DeleteJobAsync(Guid id)
    {
        var resp = await _http.DeleteAsync($"api/jobs/{id}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<JobResponseDto?> JobTransitionAsync(Guid id, string action)
    {
        var resp = await _http.PostAsync($"api/jobs/{id}/{action}", null);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JobResponseDto>(_json);
    }

    // ═══════════════════ Items ═══════════════════

    public Task<PaginatedResponse<ItemResponseDto>?> GetItemsAsync(
        string? search = null, Guid? jobId = null, Guid? kindId = null, string? status = null,
        int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<ItemResponseDto>>(
            $"api/items?search={search}&jobId={jobId}&kindId={kindId}&status={status}&page={page}&pageSize={pageSize}", _json);

    public Task<ItemResponseDto?> GetItemAsync(Guid id)
        => _http.GetFromJsonAsync<ItemResponseDto>($"api/items/{id}", _json);

    public async Task<ItemResponseDto?> CreateItemAsync(CreateItemDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/items", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ItemResponseDto>(_json);
    }

    public async Task DeleteItemAsync(Guid id)
    {
        var resp = await _http.DeleteAsync($"api/items/{id}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<ItemResponseDto?> UpdateItemAsync(Guid id, UpdateItemDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/items/{id}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ItemResponseDto>(_json);
    }

    public Task<List<ExecutionDataResponseDto>?> GetItemDataAsync(Guid id)
        => _http.GetFromJsonAsync<List<ExecutionDataResponseDto>>($"api/items/{id}/data", _json);

    public async Task<ExecutionDataResponseDto?> AddItemDataAsync(Guid id, CreateExecutionDataDto dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/items/{id}/data", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ExecutionDataResponseDto>(_json);
    }

    // ═══════════════════ Batches ═══════════════════

    public Task<PaginatedResponse<BatchResponseDto>?> GetBatchesAsync(
        string? search = null, Guid? jobId = null, Guid? kindId = null, string? status = null,
        int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<BatchResponseDto>>(
            $"api/batches?search={search}&jobId={jobId}&kindId={kindId}&status={status}&page={page}&pageSize={pageSize}", _json);

    public Task<BatchResponseDto?> GetBatchAsync(Guid id)
        => _http.GetFromJsonAsync<BatchResponseDto>($"api/batches/{id}", _json);

    public async Task<BatchResponseDto?> CreateBatchAsync(CreateBatchDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/batches", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<BatchResponseDto>(_json);
    }

    public async Task DeleteBatchAsync(Guid id)
    {
        var resp = await _http.DeleteAsync($"api/batches/{id}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<BatchResponseDto?> UpdateBatchAsync(Guid id, UpdateBatchDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/batches/{id}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<BatchResponseDto>(_json);
    }

    public async Task<BatchResponseDto?> CloseBatchAsync(Guid id)
    {
        var resp = await _http.PostAsync($"api/batches/{id}/close", null);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<BatchResponseDto>(_json);
    }

    public Task<List<ItemResponseDto>?> GetBatchItemsAsync(Guid batchId)
        => _http.GetFromJsonAsync<List<ItemResponseDto>>($"api/batches/{batchId}/items", _json);

    public async Task<ItemResponseDto?> AddBatchItemAsync(Guid batchId, Guid itemId)
    {
        var resp = await _http.PostAsync($"api/batches/{batchId}/items/{itemId}", null);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ItemResponseDto>(_json);
    }

    public async Task RemoveBatchItemAsync(Guid batchId, Guid itemId)
    {
        var resp = await _http.DeleteAsync($"api/batches/{batchId}/items/{itemId}");
        resp.EnsureSuccessStatusCode();
    }

    public Task<List<ExecutionDataResponseDto>?> GetBatchDataAsync(Guid id)
        => _http.GetFromJsonAsync<List<ExecutionDataResponseDto>>($"api/batches/{id}/data", _json);

    public async Task<ExecutionDataResponseDto?> AddBatchDataAsync(Guid id, CreateExecutionDataDto dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/batches/{id}/data", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ExecutionDataResponseDto>(_json);
    }

    // ═══════════════════ Step Executions ═══════════════════

    public Task<PaginatedResponse<StepExecutionResponseDto>?> GetStepExecutionsAsync(
        Guid? jobId = null, string? status = null, int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<StepExecutionResponseDto>>(
            $"api/step-executions?jobId={jobId}&status={status}&page={page}&pageSize={pageSize}", _json);

    public Task<StepExecutionResponseDto?> GetStepExecutionAsync(Guid id)
        => _http.GetFromJsonAsync<StepExecutionResponseDto>($"api/step-executions/{id}", _json);

    public async Task<StepExecutionResponseDto?> StepExecutionTransitionAsync(Guid id, string action)
    {
        var resp = await _http.PostAsync($"api/step-executions/{id}/{action}", null);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<StepExecutionResponseDto>(_json);
    }

    public async Task<StepExecutionResponseDto?> UpdateStepExecutionNotesAsync(Guid id, UpdateStepExecutionNotesDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/step-executions/{id}/notes", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<StepExecutionResponseDto>(_json);
    }

    public Task<List<PortTransactionResponseDto>?> GetStepExecutionPortTransactionsAsync(Guid id)
        => _http.GetFromJsonAsync<List<PortTransactionResponseDto>>($"api/step-executions/{id}/port-transactions", _json);

    public async Task<PortTransactionResponseDto?> AddStepExecutionPortTransactionAsync(Guid id, CreatePortTransactionDto dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/step-executions/{id}/port-transactions", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<PortTransactionResponseDto>(_json);
    }

    public Task<List<ExecutionDataResponseDto>?> GetStepExecutionDataAsync(Guid id)
        => _http.GetFromJsonAsync<List<ExecutionDataResponseDto>>($"api/step-executions/{id}/data", _json);

    public async Task<ExecutionDataResponseDto?> AddStepExecutionDataAsync(Guid id, CreateExecutionDataDto dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/step-executions/{id}/data", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ExecutionDataResponseDto>(_json);
    }

    // ═══════════════════ Workflows ═══════════════════

    public Task<PaginatedResponse<WorkflowResponseDto>?> GetWorkflowsAsync(
        string? search = null, bool? active = null, int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<WorkflowResponseDto>>(
            $"api/workflows?search={search}&active={active}&page={page}&pageSize={pageSize}", _json);

    public Task<WorkflowResponseDto?> GetWorkflowAsync(Guid id)
        => _http.GetFromJsonAsync<WorkflowResponseDto>($"api/workflows/{id}", _json);

    public async Task<WorkflowResponseDto?> CreateWorkflowAsync(CreateWorkflowDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/workflows", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<WorkflowResponseDto>(_json);
    }

    public async Task<WorkflowResponseDto?> UpdateWorkflowAsync(Guid id, UpdateWorkflowDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/workflows/{id}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<WorkflowResponseDto>(_json);
    }

    public async Task DeleteWorkflowAsync(Guid id)
    {
        var resp = await _http.DeleteAsync($"api/workflows/{id}");
        resp.EnsureSuccessStatusCode();
    }

    public Task<WorkflowValidationResultDto?> ValidateWorkflowAsync(Guid id)
    {
        return PostAndReadAsync<WorkflowValidationResultDto>($"api/workflows/{id}/validate");
    }

    private async Task<T?> PostAndReadAsync<T>(string url)
    {
        var resp = await _http.PostAsync(url, null);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<T>(_json);
    }

    public Task<List<WorkflowProcessResponseDto>?> GetWorkflowProcessesAsync(Guid workflowId)
        => _http.GetFromJsonAsync<List<WorkflowProcessResponseDto>>(
            $"api/workflows/{workflowId}/processes", _json);

    public async Task<WorkflowProcessResponseDto?> AddWorkflowProcessAsync(Guid workflowId, AddWorkflowProcessDto dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/workflows/{workflowId}/processes", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<WorkflowProcessResponseDto>(_json);
    }

    public async Task<WorkflowProcessResponseDto?> UpdateWorkflowProcessAsync(Guid workflowId, Guid wpId, UpdateWorkflowProcessDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/workflows/{workflowId}/processes/{wpId}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<WorkflowProcessResponseDto>(_json);
    }

    public async Task DeleteWorkflowProcessAsync(Guid workflowId, Guid wpId)
    {
        var resp = await _http.DeleteAsync($"api/workflows/{workflowId}/processes/{wpId}");
        resp.EnsureSuccessStatusCode();
    }

    public Task<List<WorkflowLinkResponseDto>?> GetWorkflowLinksAsync(Guid workflowId)
        => _http.GetFromJsonAsync<List<WorkflowLinkResponseDto>>(
            $"api/workflows/{workflowId}/links", _json);

    public async Task<WorkflowLinkResponseDto?> AddWorkflowLinkAsync(Guid workflowId, CreateWorkflowLinkDto dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/workflows/{workflowId}/links", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<WorkflowLinkResponseDto>(_json);
    }

    public async Task<WorkflowLinkResponseDto?> UpdateWorkflowLinkAsync(Guid workflowId, Guid linkId, UpdateWorkflowLinkDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/workflows/{workflowId}/links/{linkId}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<WorkflowLinkResponseDto>(_json);
    }

    public async Task DeleteWorkflowLinkAsync(Guid workflowId, Guid linkId)
    {
        var resp = await _http.DeleteAsync($"api/workflows/{workflowId}/links/{linkId}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<WorkflowLinkConditionResponseDto?> AddWorkflowLinkConditionAsync(Guid workflowId, Guid linkId, AddWorkflowLinkConditionDto dto)
    {
        var resp = await _http.PostAsJsonAsync($"api/workflows/{workflowId}/links/{linkId}/conditions", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<WorkflowLinkConditionResponseDto>(_json);
    }

    public async Task DeleteWorkflowLinkConditionAsync(Guid workflowId, Guid linkId, Guid conditionId)
    {
        var resp = await _http.DeleteAsync($"api/workflows/{workflowId}/links/{linkId}/conditions/{conditionId}");
        resp.EnsureSuccessStatusCode();
    }

    // ═══════════════════ Domain Vocabularies ═══════════════════

    public Task<PaginatedResponse<DomainVocabularyResponseDto>?> GetVocabulariesAsync(
        string? search = null, int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<DomainVocabularyResponseDto>>(
            $"api/domainvocabularies?search={search}&page={page}&pageSize={pageSize}", _json);

    public Task<DomainVocabularyResponseDto?> GetVocabularyAsync(Guid id)
        => _http.GetFromJsonAsync<DomainVocabularyResponseDto>($"api/domainvocabularies/{id}", _json);

    public async Task<DomainVocabularyResponseDto?> CreateVocabularyAsync(DomainVocabularyCreateDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/domainvocabularies", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<DomainVocabularyResponseDto>(_json);
    }

    public async Task<DomainVocabularyResponseDto?> UpdateVocabularyAsync(Guid id, DomainVocabularyUpdateDto dto)
    {
        var resp = await _http.PutAsJsonAsync($"api/domainvocabularies/{id}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<DomainVocabularyResponseDto>(_json);
    }

    public async Task DeleteVocabularyAsync(Guid id)
    {
        var resp = await _http.DeleteAsync($"api/domainvocabularies/{id}");
        resp.EnsureSuccessStatusCode();
    }

    // ══════════════════ Users (Admin) ═══════════════════════════════════════

    public Task<List<UserResponseDto>?> GetUsersAsync()
        => _http.GetFromJsonAsync<List<UserResponseDto>>("api/auth/users", _json);

    public async Task<UserResponseDto?> RegisterUserAsync(RegisterRequestDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/auth/register", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<UserResponseDto>(_json);
    }

    public async Task<UserResponseDto?> UpdateUserAsync(string id, AdminUpdateUserDto dto)
    {
        var resp = await _http.PatchAsJsonAsync($"api/auth/users/{id}", dto, _json);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<UserResponseDto>(_json);
    }

    public async Task DeleteUserAsync(string id)
    {
        var resp = await _http.DeleteAsync($"api/auth/users/{id}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task ChangePasswordAsync(ChangePasswordRequestDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/auth/change-password", dto, _json);
        resp.EnsureSuccessStatusCode();
    }

    // ══════════════════ Reports ══════════════════════════════════════════════

    public Task<ReportSummaryDto?> GetReportSummaryAsync()
        => _http.GetFromJsonAsync<ReportSummaryDto>("api/reports/summary", _json);

    public Task<List<JobStatusBreakdownDto>?> GetJobStatusBreakdownAsync()
        => _http.GetFromJsonAsync<List<JobStatusBreakdownDto>>("api/reports/job-status-breakdown", _json);

    public Task<List<StepPerformanceDto>?> GetStepPerformanceAsync()
        => _http.GetFromJsonAsync<List<StepPerformanceDto>>("api/reports/step-performance", _json);

    public Task<List<RecentCompletionDto>?> GetRecentCompletionsAsync(int count = 10)
        => _http.GetFromJsonAsync<List<RecentCompletionDto>>($"api/reports/recent-completions?count={count}", _json);

    public Task<List<ThroughputPointDto>?> GetThroughputAsync(int days = 30)
        => _http.GetFromJsonAsync<List<ThroughputPointDto>>($"api/reports/throughput?days={days}", _json);

    // ══════════════════ Alerts ═══════════════════════════════════════════════

    public Task<List<OutOfRangeAlertDto>?> GetOutOfRangeAlertsAsync(int days = 7, int limit = 100)
        => _http.GetFromJsonAsync<List<OutOfRangeAlertDto>>($"api/alerts/out-of-range?days={days}&limit={limit}", _json);

    public Task<AlertCountDto?> GetOutOfRangeAlertCountAsync(int days = 7)
        => _http.GetFromJsonAsync<AlertCountDto>($"api/alerts/out-of-range/count?days={days}", _json);

    // ══════════════════ Phase 7 — PFMEAs ═════════════════════════════════════

    public Task<PaginatedResponse<PfmeaSummaryDto>?> GetPfmeasAsync(
            string? search = null, Guid? processId = null, bool? active = null, int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<PfmeaSummaryDto>>(
            $"api/pfmeas?search={search}&processId={processId}&active={active}&page={page}&pageSize={pageSize}", _json);

    public Task<PfmeaResponseDto?> GetPfmeaAsync(Guid id)
        => _http.GetFromJsonAsync<PfmeaResponseDto>($"api/pfmeas/{id}", _json);

    public async Task<PfmeaResponseDto?> CreatePfmeaAsync(PfmeaCreateDto dto)
    {
        var r = await _http.PostAsJsonAsync("api/pfmeas", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<PfmeaResponseDto>(_json);
    }

    public async Task<PfmeaResponseDto?> UpdatePfmeaAsync(Guid id, PfmeaUpdateDto dto)
    {
        var r = await _http.PutAsJsonAsync($"api/pfmeas/{id}", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<PfmeaResponseDto>(_json);
    }

    public async Task DeletePfmeaAsync(Guid id)
    {
        var r = await _http.DeleteAsync($"api/pfmeas/{id}");
        r.EnsureSuccessStatusCode();
    }

    public async Task<PfmeaResponseDto?> BranchPfmeaAsync(Guid id)
    {
        var r = await _http.PostAsync($"api/pfmeas/{id}/branch", null);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<PfmeaResponseDto>(_json);
    }

    public async Task<PfmeaResponseDto?> AddFailureModeAsync(Guid pfmeaId, PfmeaFailureModeCreateDto dto)
    {
        var r = await _http.PostAsJsonAsync($"api/pfmeas/{pfmeaId}/failure-modes", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<PfmeaResponseDto>(_json);
    }

    public async Task<PfmeaResponseDto?> UpdateFailureModeAsync(Guid pfmeaId, Guid fmId, PfmeaFailureModeUpdateDto dto)
    {
        var r = await _http.PutAsJsonAsync($"api/pfmeas/{pfmeaId}/failure-modes/{fmId}", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<PfmeaResponseDto>(_json);
    }

    public async Task<PfmeaResponseDto?> DeleteFailureModeAsync(Guid pfmeaId, Guid fmId)
    {
        var r = await _http.DeleteAsync($"api/pfmeas/{pfmeaId}/failure-modes/{fmId}");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<PfmeaResponseDto>(_json);
    }

    public async Task<PfmeaResponseDto?> AddPfmeaActionAsync(Guid pfmeaId, Guid fmId, PfmeaActionCreateDto dto)
    {
        var r = await _http.PostAsJsonAsync($"api/pfmeas/{pfmeaId}/failure-modes/{fmId}/actions", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<PfmeaResponseDto>(_json);
    }

    public async Task<PfmeaResponseDto?> UpdatePfmeaActionAsync(Guid pfmeaId, Guid fmId, Guid actionId, PfmeaActionUpdateDto dto)
    {
        var r = await _http.PutAsJsonAsync($"api/pfmeas/{pfmeaId}/failure-modes/{fmId}/actions/{actionId}", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<PfmeaResponseDto>(_json);
    }

    public async Task<PfmeaResponseDto?> DeletePfmeaActionAsync(Guid pfmeaId, Guid fmId, Guid actionId)
    {
        var r = await _http.DeleteAsync($"api/pfmeas/{pfmeaId}/failure-modes/{fmId}/actions/{actionId}");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<PfmeaResponseDto>(_json);
    }

    // ══════════════════ Phase 7 — C&E Matrices ═══════════════════════════════

    public Task<PaginatedResponse<CeMatrixSummaryDto>?> GetCeMatricesAsync(
            string? search = null, Guid? processStepId = null, int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<CeMatrixSummaryDto>>(
            $"api/cematrices?search={search}&processStepId={processStepId}&page={page}&pageSize={pageSize}", _json);

    public Task<CeMatrixResponseDto?> GetCeMatrixAsync(Guid id)
        => _http.GetFromJsonAsync<CeMatrixResponseDto>($"api/cematrices/{id}", _json);

    public async Task<CeMatrixResponseDto?> CreateCeMatrixAsync(CeMatrixCreateDto dto)
    {
        var r = await _http.PostAsJsonAsync("api/cematrices", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CeMatrixResponseDto>(_json);
    }

    public async Task<CeMatrixResponseDto?> UpdateCeMatrixAsync(Guid id, CeMatrixUpdateDto dto)
    {
        var r = await _http.PutAsJsonAsync($"api/cematrices/{id}", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CeMatrixResponseDto>(_json);
    }

    public async Task DeleteCeMatrixAsync(Guid id)
    {
        var r = await _http.DeleteAsync($"api/cematrices/{id}");
        r.EnsureSuccessStatusCode();
    }

    public async Task<CeMatrixResponseDto?> AddCeInputAsync(Guid matrixId, CeInputCreateDto dto)
    {
        var r = await _http.PostAsJsonAsync($"api/cematrices/{matrixId}/inputs", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CeMatrixResponseDto>(_json);
    }

    public async Task<CeMatrixResponseDto?> UpdateCeInputAsync(Guid matrixId, Guid inputId, CeInputUpdateDto dto)
    {
        var r = await _http.PutAsJsonAsync($"api/cematrices/{matrixId}/inputs/{inputId}", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CeMatrixResponseDto>(_json);
    }

    public async Task<CeMatrixResponseDto?> DeleteCeInputAsync(Guid matrixId, Guid inputId)
    {
        var r = await _http.DeleteAsync($"api/cematrices/{matrixId}/inputs/{inputId}");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CeMatrixResponseDto>(_json);
    }

    public async Task<CeMatrixResponseDto?> AddCeOutputAsync(Guid matrixId, CeOutputCreateDto dto)
    {
        var r = await _http.PostAsJsonAsync($"api/cematrices/{matrixId}/outputs", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CeMatrixResponseDto>(_json);
    }

    public async Task<CeMatrixResponseDto?> UpdateCeOutputAsync(Guid matrixId, Guid outputId, CeOutputUpdateDto dto)
    {
        var r = await _http.PutAsJsonAsync($"api/cematrices/{matrixId}/outputs/{outputId}", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CeMatrixResponseDto>(_json);
    }

    public async Task<CeMatrixResponseDto?> DeleteCeOutputAsync(Guid matrixId, Guid outputId)
    {
        var r = await _http.DeleteAsync($"api/cematrices/{matrixId}/outputs/{outputId}");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CeMatrixResponseDto>(_json);
    }

    public async Task<CeMatrixResponseDto?> UpsertCorrelationAsync(Guid matrixId, CeCorrelationUpsertDto dto)
    {
        var r = await _http.PutAsJsonAsync($"api/cematrices/{matrixId}/correlations", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<CeMatrixResponseDto>(_json);
    }

    // ══════════════════ Non-Conformances ══════════════════════════════════════

    public Task<PaginatedResponse<NonConformanceResponseDto>?> GetNonConformancesAsync(
        Guid? jobId = null, Guid? stepExecutionId = null, string? status = null,
        int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<NonConformanceResponseDto>>(
            $"api/non-conformances?jobId={jobId}&stepExecutionId={stepExecutionId}&status={status}&page={page}&pageSize={pageSize}", _json);

    public Task<NonConformanceResponseDto?> GetNonConformanceAsync(Guid id)
        => _http.GetFromJsonAsync<NonConformanceResponseDto>($"api/non-conformances/{id}", _json);

    public async Task<NonConformanceResponseDto?> CreateNonConformanceAsync(CreateNonConformanceDto dto)
    {
        var r = await _http.PostAsJsonAsync("api/non-conformances", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<NonConformanceResponseDto>(_json);
    }

    public async Task<NonConformanceResponseDto?> DisposeNonConformanceAsync(Guid id, DispositionNonConformanceDto dto)
    {
        var r = await _http.PostAsJsonAsync($"api/non-conformances/{id}/dispose", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<NonConformanceResponseDto>(_json);
    }

    // ── Phase 9: Change Control ─────────────────────────────────────────────

    public Task<PaginatedResponse<ApprovalRecordResponseDto>?> GetApprovalRecordsAsync(
        string? entityType = null, Guid? entityId = null, string? decision = null, int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<ApprovalRecordResponseDto>>(
            $"api/approvals?entityType={entityType}&entityId={entityId}&decision={decision}&page={page}&pageSize={pageSize}", _json);

    public async Task<ProcessResponseDto?> SubmitProcessForApprovalAsync(Guid id, SubmitForApprovalDto dto)
    {
        var r = await _http.PostAsJsonAsync($"api/processes/{id}/submit", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ProcessResponseDto>(_json);
    }

    public async Task<ProcessResponseDto?> ApproveProcessAsync(Guid id, ApproveDto dto)
    {
        var r = await _http.PostAsJsonAsync($"api/processes/{id}/approve", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ProcessResponseDto>(_json);
    }

    public async Task<ProcessResponseDto?> RejectProcessAsync(Guid id, RejectDto dto)
    {
        var r = await _http.PostAsJsonAsync($"api/processes/{id}/reject", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ProcessResponseDto>(_json);
    }

    public async Task<ProcessResponseDto?> NewProcessRevisionAsync(Guid id, NewRevisionDto dto)
    {
        var r = await _http.PostAsJsonAsync($"api/processes/{id}/new-revision", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ProcessResponseDto>(_json);
    }

    public async Task<ProcessResponseDto?> RetireProcessAsync(Guid id)
    {
        var r = await _http.PostAsJsonAsync<object?>($"api/processes/{id}/retire", null, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ProcessResponseDto>(_json);
    }

    public async Task<StepTemplateResponseDto?> SubmitStepTemplateForApprovalAsync(Guid id, SubmitForApprovalDto dto)
    {
        var r = await _http.PostAsJsonAsync($"api/steptemplates/{id}/submit", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<StepTemplateResponseDto>(_json);
    }

    public async Task<StepTemplateResponseDto?> ApproveStepTemplateAsync(Guid id, ApproveDto dto)
    {
        var r = await _http.PostAsJsonAsync($"api/steptemplates/{id}/approve", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<StepTemplateResponseDto>(_json);
    }

    public async Task<StepTemplateResponseDto?> RejectStepTemplateAsync(Guid id, RejectDto dto)
    {
        var r = await _http.PostAsJsonAsync($"api/steptemplates/{id}/reject", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<StepTemplateResponseDto>(_json);
    }

    public async Task<StepTemplateResponseDto?> NewStepTemplateRevisionAsync(Guid id, NewRevisionDto dto)
    {
        var r = await _http.PostAsJsonAsync($"api/steptemplates/{id}/new-revision", dto, _json);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<StepTemplateResponseDto>(_json);
    }
}
