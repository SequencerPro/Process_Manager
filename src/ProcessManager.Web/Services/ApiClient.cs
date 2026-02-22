using System.Net.Http.Json;
using System.Text.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Web.Services;

/// <summary>
/// Typed HTTP client for the Process Manager API.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

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
        string? search = null, bool? active = null, int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<StepTemplateResponseDto>>(
            $"api/steptemplates?search={search}&active={active}&page={page}&pageSize={pageSize}", _json);

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
        string? search = null, bool? active = null, int page = 1, int pageSize = 25)
        => _http.GetFromJsonAsync<PaginatedResponse<ProcessSummaryResponseDto>>(
            $"api/processes?search={search}&active={active}&page={page}&pageSize={pageSize}", _json);

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
}
