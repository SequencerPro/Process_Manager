using ProcessManager.Domain.Services;

namespace ProcessManager.Tests;

/// <summary>
/// Phase 37 — pure unit tests for ModelFormatPolicy (which formats are web-ready
/// vs. need server-side STEP→glTF conversion).
/// </summary>
public class ModelFormatPolicyTests
{
    [Theory]
    [InlineData("part.glb")]
    [InlineData("part.gltf")]
    [InlineData("part.stl")]
    [InlineData("part.obj")]
    public void WebReadyFormats_NeedNoConversion(string fileName)
    {
        var c = ModelFormatPolicy.Classify(fileName);
        Assert.True(c.IsSupported);
        Assert.True(c.IsWebReady);
        Assert.False(c.NeedsConversion);
        Assert.Equal(ModelConversionStatus.NotRequired, c.InitialStatus);
    }

    [Theory]
    [InlineData("assembly.step")]
    [InlineData("assembly.stp")]
    [InlineData("assembly.iges")]
    [InlineData("assembly.igs")]
    public void CadFormats_NeedConversion(string fileName)
    {
        var c = ModelFormatPolicy.Classify(fileName);
        Assert.True(c.IsSupported);
        Assert.False(c.IsWebReady);
        Assert.True(c.NeedsConversion);
        Assert.Equal(ModelConversionStatus.Pending, c.InitialStatus);
    }

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("image.png")]
    [InlineData("archive.zip")]
    [InlineData("")]
    public void UnsupportedFormats_AreRejected(string fileName)
    {
        var c = ModelFormatPolicy.Classify(fileName);
        Assert.False(c.IsSupported);
        Assert.Equal(ModelConversionStatus.None, c.InitialStatus);
        Assert.False(ModelFormatPolicy.IsAllowed(fileName));
    }

    [Fact]
    public void Classify_IsCaseInsensitive_AndHandlesBareExtensions()
    {
        Assert.True(ModelFormatPolicy.Classify("PART.STEP").NeedsConversion);
        Assert.True(ModelFormatPolicy.Classify(".GLB").IsWebReady);
        Assert.True(ModelFormatPolicy.Classify("glb").IsWebReady); // no dot
    }

    [Fact]
    public void ConvertedTarget_IsGlb()
    {
        Assert.Equal(".glb", ModelFormatPolicy.ConvertedExtension);
        Assert.Equal("model/gltf-binary", ModelFormatPolicy.ConvertedMimeType);
    }

    [Theory]
    [InlineData("a.glb", "model/gltf-binary")]
    [InlineData("a.gltf", "model/gltf+json")]
    [InlineData("a.stl", "model/stl")]
    [InlineData("a.step", "application/step")]
    [InlineData("a.iges", "application/iges")]
    [InlineData("a.xyz", "application/octet-stream")]
    public void MimeTypeFor_MapsExtensions(string fileName, string expected)
    {
        Assert.Equal(expected, ModelFormatPolicy.MimeTypeFor(fileName));
    }

    [Fact]
    public void AllowedExtensions_CoverAllSupportedFormats()
    {
        var allowed = ModelFormatPolicy.AllowedExtensions;
        Assert.Contains(".glb", allowed);
        Assert.Contains(".step", allowed);
        Assert.Contains(".iges", allowed);
        Assert.DoesNotContain(".txt", allowed);
    }
}
