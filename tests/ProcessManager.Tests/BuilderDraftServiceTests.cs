using ProcessManager.Web.Services;

namespace ProcessManager.Tests;

/// <summary>
/// Tests for <see cref="BuilderDraftService"/> using an in-memory
/// <see cref="IBuilderDraftStore"/> fake — exercises the round-trip,
/// stale-draft drop, corrupted-payload recovery, and clear semantics
/// without touching IJSRuntime.
/// </summary>
public class BuilderDraftServiceTests
{
    private sealed class InMemoryStore : IBuilderDraftStore
    {
        public readonly Dictionary<string, DraftEnvelope> Backing = new();

        public Task SaveAsync(DraftEnvelope envelope, CancellationToken ct = default)
        {
            Backing[Key(envelope.EntityKind, envelope.EntityId)] = envelope;
            return Task.CompletedTask;
        }

        public Task<DraftEnvelope?> LoadAsync(string entityKind, Guid entityId, CancellationToken ct = default)
            => Task.FromResult(Backing.TryGetValue(Key(entityKind, entityId), out var v) ? v : null);

        public Task ClearAsync(string entityKind, Guid entityId, CancellationToken ct = default)
        {
            Backing.Remove(Key(entityKind, entityId));
            return Task.CompletedTask;
        }

        private static string Key(string k, Guid id) => $"{k}:{id}";
    }

    private sealed class FixedClock : TimeProvider
    {
        private DateTimeOffset _now;
        public FixedClock(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }

    private sealed record Model(string Title, int Count);

    [Fact]
    public async Task Save_then_Load_round_trips_the_model()
    {
        var store = new InMemoryStore();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var svc = new BuilderDraftService(store, clock);
        var id = Guid.NewGuid();

        await svc.SaveDraftAsync("Process", id, new Model("Hello", 7), subject: "demo");

        var recovered = await svc.LoadDraftAsync<Model>("Process", id);
        Assert.NotNull(recovered);
        Assert.Equal("Hello", recovered!.Model.Title);
        Assert.Equal(7, recovered.Model.Count);
        Assert.Equal("demo", recovered.Subject);
    }

    [Fact]
    public async Task Load_returns_null_when_no_draft_exists()
    {
        var svc = new BuilderDraftService(new InMemoryStore());
        var recovered = await svc.LoadDraftAsync<Model>("Process", Guid.NewGuid());
        Assert.Null(recovered);
    }

    [Fact]
    public async Task Stale_draft_older_than_server_is_dropped_silently()
    {
        var store = new InMemoryStore();
        var clock = new FixedClock(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var svc = new BuilderDraftService(store, clock);
        var id = Guid.NewGuid();

        await svc.SaveDraftAsync("Process", id, new Model("Old", 1));

        // Server modified AFTER the draft was saved.
        var serverModified = clock.GetUtcNow().AddMinutes(5);

        var recovered = await svc.LoadDraftAsync<Model>("Process", id, serverModified);
        Assert.Null(recovered);

        // The stale envelope should have been cleared from the store.
        Assert.Empty(store.Backing);
    }

    [Fact]
    public async Task Draft_newer_than_server_is_kept()
    {
        var store = new InMemoryStore();
        var clock = new FixedClock(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var svc = new BuilderDraftService(store, clock);
        var id = Guid.NewGuid();

        // Server's last-modified is BEFORE the draft.
        var serverModified = clock.GetUtcNow().AddMinutes(-5);
        await svc.SaveDraftAsync("Process", id, new Model("New", 2));

        var recovered = await svc.LoadDraftAsync<Model>("Process", id, serverModified);
        Assert.NotNull(recovered);
        Assert.Equal("New", recovered!.Model.Title);
    }

    [Fact]
    public async Task Corrupted_payload_is_dropped_and_returns_null()
    {
        var store = new InMemoryStore();
        var id = Guid.NewGuid();
        store.Backing[$"Process:{id}"] = new DraftEnvelope(
            "Process", id, "{not valid json", DateTimeOffset.UtcNow);

        var svc = new BuilderDraftService(store);

        var recovered = await svc.LoadDraftAsync<Model>("Process", id);
        Assert.Null(recovered);
        Assert.Empty(store.Backing); // corrupted draft was cleared
    }

    [Fact]
    public async Task ClearDraft_removes_entry()
    {
        var store = new InMemoryStore();
        var svc = new BuilderDraftService(store);
        var id = Guid.NewGuid();

        await svc.SaveDraftAsync("Workflow", id, new Model("x", 0));
        await svc.ClearDraftAsync("Workflow", id);

        Assert.Empty(store.Backing);
    }

    [Fact]
    public async Task Save_rejects_empty_entityKind()
    {
        var svc = new BuilderDraftService(new InMemoryStore());
        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.SaveDraftAsync(" ", Guid.NewGuid(), new Model("a", 1)));
    }

    [Fact]
    public async Task Save_rejects_empty_entityId()
    {
        var svc = new BuilderDraftService(new InMemoryStore());
        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.SaveDraftAsync("Process", Guid.Empty, new Model("a", 1)));
    }

    [Fact]
    public void Ctor_rejects_null_store()
    {
        Assert.Throws<ArgumentNullException>(() => new BuilderDraftService(null!));
    }
}
