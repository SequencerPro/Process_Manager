extern alias Web;

using Web::ProcessManager.Web.Services;

namespace ProcessManager.Tests;

/// <summary>
/// The Web project's <see cref="ApiClient"/> builds list URLs by string interpolation.
/// Search text typed by a user must reach the API unchanged, even when it contains
/// characters that have a meaning in a query string.
/// </summary>
public class ApiClientSearchEncodingTests : IntegrationTestBase
{
    private readonly ApiClient _api;

    public ApiClientSearchEncodingTests(TestWebApplicationFactory factory) : base(factory)
    {
        _api = new ApiClient(Client, JsonOptions);
    }

    // {0} is replaced with a unique prefix. The decoy matches what the API would
    // receive if the search text were cut off at the special character.
    [Theory]
    [InlineData("Nuts & Bolts {0}", "Nuts {0}")]
    [InlineData("Bin #7 {0}", "Bin {0}")]
    [InlineData("C++ {0}", "C {0}")]
    public async Task GetKinds_SearchWithQueryStringCharacters_FindsOnlyExactMatch(string nameFormat, string decoyFormat)
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var name = string.Format(nameFormat, pfx);
        var kind = await CreateKind($"ENC-{pfx}", name);
        await CreateKind($"DEC-{pfx}", string.Format(decoyFormat, pfx));

        var result = await _api.GetKindsAsync(name);

        Assert.NotNull(result);
        var only = Assert.Single(result!.Items);
        Assert.Equal(kind.Id, only.Id);
    }

    [Fact]
    public async Task GetKinds_PlainSearch_FindsMatch()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var kind = await CreateKind($"PLN-{pfx}", $"Plain {pfx}");

        var result = await _api.GetKindsAsync($"Plain {pfx}");

        Assert.NotNull(result);
        Assert.Equal(kind.Id, Assert.Single(result!.Items).Id);
    }

    [Fact]
    public async Task GetKinds_NullSearch_ReturnsUnfilteredList()
    {
        var pfx = Guid.NewGuid().ToString()[..6];
        var kind = await CreateKind($"ALL-{pfx}", $"Unfiltered {pfx}");

        var result = await _api.GetKindsAsync(search: null, pageSize: 1000);

        Assert.NotNull(result);
        Assert.Contains(result!.Items, k => k.Id == kind.Id);
    }
}
