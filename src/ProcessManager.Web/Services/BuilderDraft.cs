using System.Text.Json;
using Microsoft.JSInterop;

namespace ProcessManager.Web.Services;

/// <summary>
/// One unsaved-draft record. Carries enough metadata to drive the
/// "Recover unsaved changes?" banner shown when a builder reopens.
/// </summary>
public sealed record DraftEnvelope(
    string EntityKind,
    Guid EntityId,
    string Payload,
    DateTimeOffset SavedAtUtc,
    string? Subject = null);

/// <summary>
/// Storage abstraction for builder drafts. Real implementation backs onto
/// browser localStorage via IJSRuntime; tests provide an in-memory fake.
/// </summary>
public interface IBuilderDraftStore
{
    Task SaveAsync(DraftEnvelope envelope, CancellationToken ct = default);
    Task<DraftEnvelope?> LoadAsync(string entityKind, Guid entityId, CancellationToken ct = default);
    Task ClearAsync(string entityKind, Guid entityId, CancellationToken ct = default);
}

/// <summary>
/// Builder-facing facade. Components call <see cref="SaveDraftAsync{T}"/>
/// debounced on every mutation; on load they call <see cref="LoadDraftAsync{T}"/>
/// to decide whether to show the recovery banner.
/// </summary>
public class BuilderDraftService
{
    private readonly IBuilderDraftStore _store;
    private readonly TimeProvider _clock;

    public BuilderDraftService(IBuilderDraftStore store, TimeProvider? clock = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _clock = clock ?? TimeProvider.System;
    }

    public async Task SaveDraftAsync<T>(
        string entityKind, Guid entityId, T model,
        string? subject = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entityKind))
            throw new ArgumentException("entityKind is required", nameof(entityKind));
        if (entityId == Guid.Empty)
            throw new ArgumentException("entityId is required", nameof(entityId));

        var payload = JsonSerializer.Serialize(model);
        var envelope = new DraftEnvelope(
            entityKind, entityId, payload,
            _clock.GetUtcNow(), subject);

        await _store.SaveAsync(envelope, ct);
    }

    public async Task<DraftRecovery<T>?> LoadDraftAsync<T>(
        string entityKind, Guid entityId,
        DateTimeOffset? serverLastModifiedUtc = null,
        CancellationToken ct = default)
    {
        var envelope = await _store.LoadAsync(entityKind, entityId, ct);
        if (envelope is null) return null;

        // If the server copy is newer than (or equal to) the draft, the draft
        // is stale — drop it silently. This avoids showing the banner after
        // a successful save that came from a different tab/device.
        if (serverLastModifiedUtc.HasValue &&
            envelope.SavedAtUtc <= serverLastModifiedUtc.Value)
        {
            await _store.ClearAsync(entityKind, entityId, ct);
            return null;
        }

        T? model;
        try
        {
            model = JsonSerializer.Deserialize<T>(envelope.Payload);
        }
        catch (JsonException)
        {
            // Corrupted draft — clear it so we don't keep failing on every open.
            await _store.ClearAsync(entityKind, entityId, ct);
            return null;
        }

        if (model is null) return null;

        return new DraftRecovery<T>(model, envelope.SavedAtUtc, envelope.Subject);
    }

    public Task ClearDraftAsync(string entityKind, Guid entityId, CancellationToken ct = default)
        => _store.ClearAsync(entityKind, entityId, ct);
}

/// <summary>
/// Result of loading a draft — model plus the metadata needed to render
/// the recovery banner ("Draft from 14 minutes ago — Recover / Discard").
/// </summary>
public sealed record DraftRecovery<T>(
    T Model,
    DateTimeOffset SavedAtUtc,
    string? Subject);

/// <summary>
/// IJSRuntime-backed store. Persists under "pmDraft:{kind}:{id}" in
/// localStorage so drafts survive a tab close.
/// </summary>
public class LocalStorageBuilderDraftStore : IBuilderDraftStore
{
    private readonly IJSRuntime _js;
    public LocalStorageBuilderDraftStore(IJSRuntime js) => _js = js;

    private static string Key(string kind, Guid id) => $"pmDraft:{kind}:{id}";

    public async Task SaveAsync(DraftEnvelope envelope, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(envelope);
        await _js.InvokeVoidAsync("localStorage.setItem", ct, Key(envelope.EntityKind, envelope.EntityId), json);
    }

    public async Task<DraftEnvelope?> LoadAsync(string entityKind, Guid entityId, CancellationToken ct = default)
    {
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", ct, Key(entityKind, entityId));
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<DraftEnvelope>(json); }
        catch (JsonException) { return null; }
    }

    public async Task ClearAsync(string entityKind, Guid entityId, CancellationToken ct = default)
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", ct, Key(entityKind, entityId));
    }
}
