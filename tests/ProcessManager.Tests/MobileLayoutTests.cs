using System.Reflection;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace ProcessManager.Tests;

public class MobileLayoutTests
{
    private static readonly Assembly WebAssembly =
        typeof(ProcessManager.Web.Components.Layout.MainLayout).Assembly;

    private static Type GetType(string name) =>
        WebAssembly.GetTypes().First(t => t.Name == name);

    [Fact]
    public void NavMenu_HasIsOpenParameter()
    {
        var type = GetType("NavMenu");
        var prop = type.GetProperty("IsOpen", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop!.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());
    }

    [Fact]
    public void NavMenu_HasOnCloseParameter()
    {
        var type = GetType("NavMenu");
        var prop = type.GetProperty("OnClose", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.Equal(typeof(EventCallback), prop!.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<ParameterAttribute>());
    }

    [Fact]
    public void NavMenu_HasIsOpenField()
    {
        var type = GetType("NavMenu");
        var field = type.GetField("_isOpen", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.Equal(typeof(bool), field!.FieldType);
    }

    [Fact]
    public void NavMenu_HasToggleOpenMethod()
    {
        var type = GetType("NavMenu");
        var method = type.GetMethod("ToggleOpen", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    [Fact]
    public void NavMenu_HasCloseNavMethod()
    {
        var type = GetType("NavMenu");
        var method = type.GetMethod("CloseNav", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }
}
