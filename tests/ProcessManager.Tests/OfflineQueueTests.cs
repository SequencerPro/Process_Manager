using System.Net;
using System.Net.Http.Json;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Tests;

public class OfflineQueueTests : IntegrationTestBase
{
    public OfflineQueueTests(TestWebApplicationFactory factory) : base(factory) { }

    private async Task<(Guid StepExecutionId, Guid ContentBlockId)> SetupStepWithPrompt(
        string promptType = "NumericEntry", decimal? min = null, decimal? max = null)
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        var promptDto = new AddPromptBlockDto("Test Prompt", promptType, true, "units", min, max);
        var promptResp = await Client.PostAsJsonAsync(
            $"/api/processes/{scenario.Process.Id}/steps/{scenario.ProcessStep1.Id}/content/prompt",
            promptDto, JsonOptions);
        promptResp.EnsureSuccessStatusCode();
        var contentBlock = await promptResp.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(JsonOptions);

        return (step1.Id, contentBlock!.Id);
    }

    [Fact]
    public async Task OfflineQueue_FlushesInOrder_ViaSequentialBatchCalls()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        var prompt1 = new AddPromptBlockDto("Torque", "NumericEntry", true, "Nm", 20, 30);
        var resp1 = await Client.PostAsJsonAsync(
            $"/api/processes/{scenario.Process.Id}/steps/{scenario.ProcessStep1.Id}/content/prompt",
            prompt1, JsonOptions);
        var block1 = (await resp1.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(JsonOptions))!;

        var prompt2 = new AddPromptBlockDto("Angle", "NumericEntry", true, "deg", 0, 360);
        var resp2 = await Client.PostAsJsonAsync(
            $"/api/processes/{scenario.Process.Id}/steps/{scenario.ProcessStep1.Id}/content/prompt",
            prompt2, JsonOptions);
        var block2 = (await resp2.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(JsonOptions))!;

        var prompt3 = new AddPromptBlockDto("Pressure", "NumericEntry", true, "psi", 0, 100);
        var resp3 = await Client.PostAsJsonAsync(
            $"/api/processes/{scenario.Process.Id}/steps/{scenario.ProcessStep1.Id}/content/prompt",
            prompt3, JsonOptions);
        var block3 = (await resp3.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(JsonOptions))!;

        var batch1 = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(Guid.NewGuid().ToString(), block1.Id, null, "25.0")
        });
        var batch2 = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(Guid.NewGuid().ToString(), block2.Id, null, "180.0")
        });
        var batch3 = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(Guid.NewGuid().ToString(), block3.Id, null, "50.0")
        });

        (await Client.PostAsJsonAsync($"/api/step-executions/{step1.Id}/prompt-responses/batch", batch1, JsonOptions)).EnsureSuccessStatusCode();
        (await Client.PostAsJsonAsync($"/api/step-executions/{step1.Id}/prompt-responses/batch", batch2, JsonOptions)).EnsureSuccessStatusCode();
        (await Client.PostAsJsonAsync($"/api/step-executions/{step1.Id}/prompt-responses/batch", batch3, JsonOptions)).EnsureSuccessStatusCode();

        var responses = await Client.GetFromJsonAsync<List<PromptResponseDto>>(
            $"/api/step-executions/{step1.Id}/prompt-responses", JsonOptions);

        Assert.Equal(3, responses!.Count);
        Assert.Contains(responses, r => r.ProcessStepContentId == block1.Id && r.ResponseValue == "25.0");
        Assert.Contains(responses, r => r.ProcessStepContentId == block2.Id && r.ResponseValue == "180.0");
        Assert.Contains(responses, r => r.ProcessStepContentId == block3.Id && r.ResponseValue == "50.0");
    }

    [Fact]
    public async Task OfflineQueue_ClientIdIdempotency_PreventsDuplicateWrites()
    {
        var (seId, blockId) = await SetupStepWithPrompt(min: 20, max: 30);

        var clientId = Guid.NewGuid().ToString();
        var batch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(clientId, blockId, null, "25.0")
        });

        for (int i = 0; i < 5; i++)
        {
            var resp = await Client.PostAsJsonAsync(
                $"/api/step-executions/{seId}/prompt-responses/batch", batch, JsonOptions);
            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        }

        var responses = await Client.GetFromJsonAsync<List<PromptResponseDto>>(
            $"/api/step-executions/{seId}/prompt-responses", JsonOptions);

        var matching = responses!.Where(r => r.ProcessStepContentId == blockId).ToList();
        Assert.Single(matching);
        Assert.Equal("25.0", matching[0].ResponseValue);
    }

    [Fact]
    public async Task OfflineQueue_ScanPromptType_AcceptsTextValue()
    {
        var (seId, blockId) = await SetupStepWithPrompt("Scan");

        var batch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(Guid.NewGuid().ToString(), blockId, null, "ABC123456789")
        });

        var resp = await Client.PostAsJsonAsync(
            $"/api/step-executions/{seId}/prompt-responses/batch", batch, JsonOptions);
        resp.EnsureSuccessStatusCode();

        var responses = await Client.GetFromJsonAsync<List<PromptResponseDto>>(
            $"/api/step-executions/{seId}/prompt-responses", JsonOptions);

        var matching = responses!.First(r => r.ProcessStepContentId == blockId);
        Assert.Equal("ABC123456789", matching.ResponseValue);
    }

    [Fact]
    public async Task OfflineQueue_TextEntryPrompt_AcceptsKeyboardWedgeInput()
    {
        var (seId, blockId) = await SetupStepWithPrompt("TextEntry");

        var barcodeValue = "4901234567890";
        var batch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(Guid.NewGuid().ToString(), blockId, null, barcodeValue)
        });

        var resp = await Client.PostAsJsonAsync(
            $"/api/step-executions/{seId}/prompt-responses/batch", batch, JsonOptions);
        resp.EnsureSuccessStatusCode();

        var responses = await Client.GetFromJsonAsync<List<PromptResponseDto>>(
            $"/api/step-executions/{seId}/prompt-responses", JsonOptions);

        var matching = responses!.First(r => r.ProcessStepContentId == blockId);
        Assert.Equal(barcodeValue, matching.ResponseValue);
    }

    [Fact]
    public async Task BatchPromptResponses_LargeOfflineBatch_ProcessesAllItems()
    {
        var scenario = await BuildWidgetFinishingScenario();
        var job = await CreateJob(scenario.Process.Id);
        await Client.PostAsync($"/api/jobs/{job.Id}/start", null);

        var executions = await Client.GetFromJsonAsync<List<StepExecutionResponseDto>>(
            $"/api/jobs/{job.Id}/step-executions", JsonOptions);
        var step1 = executions!.First(se => se.Sequence == 1);
        await Client.PostAsync($"/api/step-executions/{step1.Id}/start", null);

        var blockIds = new List<Guid>();
        for (int i = 0; i < 10; i++)
        {
            var prompt = new AddPromptBlockDto($"Measurement {i + 1}", "NumericEntry", true, "mm", 0, 100);
            var resp = await Client.PostAsJsonAsync(
                $"/api/processes/{scenario.Process.Id}/steps/{scenario.ProcessStep1.Id}/content/prompt",
                prompt, JsonOptions);
            var block = (await resp.Content.ReadFromJsonAsync<ProcessStepContentResponseDto>(JsonOptions))!;
            blockIds.Add(block.Id);
        }

        var items = blockIds.Select((id, idx) =>
            new BatchPromptResponseItemDto(Guid.NewGuid().ToString(), id, null, $"{50.0 + idx}")
        ).ToList();

        var batch = new BatchPromptResponsesDto(items);
        var batchResp = await Client.PostAsJsonAsync(
            $"/api/step-executions/{step1.Id}/prompt-responses/batch", batch, JsonOptions);
        batchResp.EnsureSuccessStatusCode();

        var responses = await Client.GetFromJsonAsync<List<PromptResponseDto>>(
            $"/api/step-executions/{step1.Id}/prompt-responses", JsonOptions);

        Assert.Equal(10, responses!.Count);
    }

    [Fact]
    public void ServiceWorker_FileExists_WithExpectedContent()
    {
        var path = Path.Combine(FindWebRoot(), "operator-sw.js");
        Assert.True(File.Exists(path), "operator-sw.js should exist in wwwroot");
        var content = File.ReadAllText(path);
        // Version-agnostic: the cache name is bumped on each SW change
        // (pm-operator-v1, v2, ...). Assert the stable prefix so a routine
        // cache bump doesn't break this test.
        Assert.Contains("pm-operator-v", content);
        Assert.Contains("self.addEventListener", content);
    }

    [Fact]
    public void OperatorSyncJs_FileExists_WithExpectedContent()
    {
        var path = Path.Combine(FindWebRoot(), "js", "operator-sync.js");
        Assert.True(File.Exists(path), "operator-sync.js should exist in wwwroot/js");
        var content = File.ReadAllText(path);
        Assert.Contains("ProcessManagerOfflineQueue", content);
        Assert.Contains("OperatorSync", content);
        Assert.Contains("flushQueue", content);
    }

    [Fact]
    public void BarcodeScannerJs_FileExists_WithExpectedContent()
    {
        var path = Path.Combine(FindWebRoot(), "js", "barcode-scanner.js");
        Assert.True(File.Exists(path), "barcode-scanner.js should exist in wwwroot/js");
        var content = File.ReadAllText(path);
        Assert.Contains("BarcodeScanner", content);
        Assert.Contains("startScan", content);
    }

    [Fact]
    public void PhotoCaptureJs_FileExists_WithExpectedContent()
    {
        var path = Path.Combine(FindWebRoot(), "js", "photo-capture.js");
        Assert.True(File.Exists(path), "photo-capture.js should exist in wwwroot/js");
        var content = File.ReadAllText(path);
        Assert.Contains("PhotoCapture", content);
        Assert.Contains("compressImage", content);
    }

    [Fact]
    public void SignaturePadJs_FileExists_WithExpectedContent()
    {
        var path = Path.Combine(FindWebRoot(), "js", "signature-pad.js");
        Assert.True(File.Exists(path), "signature-pad.js should exist in wwwroot/js");
        var content = File.ReadAllText(path);
        Assert.Contains("SignaturePad", content);
        Assert.Contains("quadraticCurveTo", content);
    }

    private static string FindWebRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "src", "ProcessManager.Web", "wwwroot");
            if (Directory.Exists(candidate)) return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }
        var fallback = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "src", "ProcessManager.Web", "wwwroot"));
        return fallback;
    }

    [Fact]
    public async Task BatchPromptResponses_MixedOfflineOnline_MergesCorrectly()
    {
        var (seId, blockId) = await SetupStepWithPrompt(min: 20, max: 30);

        var onlineClientId = Guid.NewGuid().ToString();
        var onlineBatch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(onlineClientId, blockId, null, "22.0")
        });
        (await Client.PostAsJsonAsync(
            $"/api/step-executions/{seId}/prompt-responses/batch", onlineBatch, JsonOptions)).EnsureSuccessStatusCode();

        var offlineClientId = Guid.NewGuid().ToString();
        var offlineBatch = new BatchPromptResponsesDto(new List<BatchPromptResponseItemDto>
        {
            new(offlineClientId, blockId, null, "26.0")
        });
        (await Client.PostAsJsonAsync(
            $"/api/step-executions/{seId}/prompt-responses/batch", offlineBatch, JsonOptions)).EnsureSuccessStatusCode();

        var responses = await Client.GetFromJsonAsync<List<PromptResponseDto>>(
            $"/api/step-executions/{seId}/prompt-responses", JsonOptions);

        var matching = responses!.Where(r => r.ProcessStepContentId == blockId).ToList();
        Assert.Single(matching);
        Assert.Equal("26.0", matching[0].ResponseValue);
    }
}
