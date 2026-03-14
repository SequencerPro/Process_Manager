using Bunit;
using ProcessManager.Web.Components.Shared;

namespace ProcessManager.Tests;

public class StepConnectorTests : TestContext
{
    [Fact]
    public void Renders_StepConnector_CssClass()
    {
        var cut = RenderComponent<StepConnector>(parameters => parameters
            .Add(p => p.Status, StepConnectionStatus.Incompatible));

        var wrapper = cut.Find(".step-connector");
        Assert.NotNull(wrapper);
    }

    [Fact]
    public void Connected_Status_Shows_Success_Badge_With_Port_Names()
    {
        var cut = RenderComponent<StepConnector>(parameters => parameters
            .Add(p => p.Status, StepConnectionStatus.Connected)
            .Add(p => p.SourcePortName, "Output-A")
            .Add(p => p.TargetPortName, "Input-B")
            .Add(p => p.PortInfoText, "Widget / Grade1"));

        var badge = cut.Find(".badge.bg-success-subtle");
        Assert.NotNull(badge);
        Assert.Contains("Output-A", badge.TextContent);
        Assert.Contains("Input-B", badge.TextContent);
        Assert.Contains("Widget / Grade1", badge.TextContent);
    }

    [Fact]
    public void Connected_Status_Shows_Check_Circle_Icon()
    {
        var cut = RenderComponent<StepConnector>(parameters => parameters
            .Add(p => p.Status, StepConnectionStatus.Connected)
            .Add(p => p.SourcePortName, "Out")
            .Add(p => p.TargetPortName, "In"));

        var icon = cut.Find(".bi-check-circle");
        Assert.NotNull(icon);
    }

    [Fact]
    public void Connected_Status_Omits_PortInfo_When_Null()
    {
        var cut = RenderComponent<StepConnector>(parameters => parameters
            .Add(p => p.Status, StepConnectionStatus.Connected)
            .Add(p => p.SourcePortName, "Out")
            .Add(p => p.TargetPortName, "In")
            .Add(p => p.PortInfoText, null));

        // Should not render the parenthetical span
        var spans = cut.FindAll(".step-connector .badge span.opacity-75");
        Assert.Empty(spans);
    }

    [Fact]
    public void Incompatible_Status_Shows_Danger_Badge()
    {
        var cut = RenderComponent<StepConnector>(parameters => parameters
            .Add(p => p.Status, StepConnectionStatus.Incompatible));

        var badge = cut.Find(".badge.bg-danger-subtle");
        Assert.NotNull(badge);
        Assert.Contains("No compatible connection", badge.TextContent);
    }

    [Fact]
    public void Incompatible_Status_Shows_X_Circle_Icon()
    {
        var cut = RenderComponent<StepConnector>(parameters => parameters
            .Add(p => p.Status, StepConnectionStatus.Incompatible));

        var icon = cut.Find(".bi-x-circle");
        Assert.NotNull(icon);
    }

    [Fact]
    public void Ambiguous_Status_Shows_Warning_With_ChildContent()
    {
        var cut = RenderComponent<StepConnector>(parameters => parameters
            .Add(p => p.Status, StepConnectionStatus.Ambiguous)
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenElement(0, "select");
                builder.AddAttribute(1, "class", "test-select");
                builder.OpenElement(2, "option");
                builder.AddContent(3, "Option1");
                builder.CloseElement();
                builder.CloseElement();
            }));

        var warning = cut.Find(".bi-exclamation-triangle-fill");
        Assert.NotNull(warning);

        var select = cut.Find("select.test-select");
        Assert.NotNull(select);
        Assert.Contains("Option1", select.TextContent);
    }

    [Fact]
    public void Ambiguous_Status_Does_Not_Show_Success_Or_Danger_Badge()
    {
        var cut = RenderComponent<StepConnector>(parameters => parameters
            .Add(p => p.Status, StepConnectionStatus.Ambiguous));

        Assert.Empty(cut.FindAll(".badge.bg-success-subtle"));
        Assert.Empty(cut.FindAll(".badge.bg-danger-subtle"));
    }

    [Fact]
    public void InsertButton_Renders_When_Callback_Provided()
    {
        var clicked = false;
        var cut = RenderComponent<StepConnector>(parameters => parameters
            .Add(p => p.Status, StepConnectionStatus.Incompatible)
            .Add(p => p.OnInsertClick, () => { clicked = true; }));

        var btn = cut.Find(".step-connector button.btn-outline-primary");
        Assert.NotNull(btn);
        Assert.Contains("Insert Step Here", btn.TextContent);

        btn.Click();
        Assert.True(clicked);
    }

    [Fact]
    public void InsertButton_Does_Not_Render_Without_Callback()
    {
        var cut = RenderComponent<StepConnector>(parameters => parameters
            .Add(p => p.Status, StepConnectionStatus.Incompatible));

        var buttons = cut.FindAll(".step-connector button.btn-outline-primary");
        Assert.Empty(buttons);
    }

    [Fact]
    public void No_Old_ArrowDown_Icons_Present()
    {
        var cut = RenderComponent<StepConnector>(parameters => parameters
            .Add(p => p.Status, StepConnectionStatus.Connected)
            .Add(p => p.SourcePortName, "Out")
            .Add(p => p.TargetPortName, "In")
            .Add(p => p.OnInsertClick, () => { }));

        // The old implementation used bi-arrow-down icons; these should NOT exist
        var arrowDownIcons = cut.FindAll(".bi-arrow-down");
        Assert.Empty(arrowDownIcons);
    }

    [Theory]
    [InlineData(StepConnectionStatus.Connected)]
    [InlineData(StepConnectionStatus.Ambiguous)]
    [InlineData(StepConnectionStatus.Incompatible)]
    public void All_Statuses_Render_StepConnector_Wrapper(StepConnectionStatus status)
    {
        var cut = RenderComponent<StepConnector>(parameters => parameters
            .Add(p => p.Status, status)
            .Add(p => p.SourcePortName, "Out")
            .Add(p => p.TargetPortName, "In"));

        // All statuses should use the step-connector CSS class for the arrow styling
        var wrapper = cut.Find(".step-connector");
        Assert.NotNull(wrapper);
        Assert.Equal("DIV", wrapper.TagName);
    }

    [Fact]
    public void Connected_Status_Does_Not_Show_Danger_Or_Warning()
    {
        var cut = RenderComponent<StepConnector>(parameters => parameters
            .Add(p => p.Status, StepConnectionStatus.Connected)
            .Add(p => p.SourcePortName, "Out")
            .Add(p => p.TargetPortName, "In"));

        Assert.Empty(cut.FindAll(".badge.bg-danger-subtle"));
        Assert.Empty(cut.FindAll(".bi-exclamation-triangle-fill"));
    }

    [Fact]
    public void Incompatible_Status_Does_Not_Show_Success_Or_Warning()
    {
        var cut = RenderComponent<StepConnector>(parameters => parameters
            .Add(p => p.Status, StepConnectionStatus.Incompatible));

        Assert.Empty(cut.FindAll(".badge.bg-success-subtle"));
        Assert.Empty(cut.FindAll(".bi-exclamation-triangle-fill"));
    }
}
