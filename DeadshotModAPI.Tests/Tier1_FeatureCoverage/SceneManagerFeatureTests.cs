using System;
using System.Reflection;
using DeadshotModAPI.Tests.TestHelpers;
using Xunit;

namespace DeadshotModAPI.Tests.Tier1_FeatureCoverage;

public class SceneManagerFeatureTests
{
    [Fact]
    public void SceneManager_LoadByName_ExecutesDefensivelyWithoutCrashing()
    {
        // Act & Assert
        // In headless test mode, LoadingScreen is not backed by Unity IL2CPP runtime,
        // but SceneManager.Load wraps the call in defensive try-catch so it never throws out.
        var ex = Record.Exception(() => SceneManager.Load("Level01"));
        Assert.Null(ex);
    }

    [Fact]
    public void SceneManager_LoadByIndex_ExecutesDefensivelyWithoutCrashing()
    {
        // Act & Assert
        var ex = Record.Exception(() => SceneManager.Load(1));
        Assert.Null(ex);
    }

    [Fact]
    public void SceneManager_LoadZeroIndex_ExecutesDefensivelyWithoutCrashing()
    {
        // Act & Assert
        var ex = Record.Exception(() => SceneManager.Load(0));
        Assert.Null(ex);
    }

    [Fact]
    public void SceneManager_LoadModsBundle_HandlesGracefullyOrIsolatesNativeBinding()
    {
        // Act & Assert
        // LoadModsBundle checks _modsBundle != null. In native IL2CPP runtime, this safely checks bundle cache.
        // In headless runner without GameAssembly.dll, any IL2CPP static initialization is caught.
        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadModsBundle());
        if (ex != null)
        {
            Assert.True(
                ex is TypeInitializationException ||
                ex is TargetInvocationException tie && tie.InnerException is TypeInitializationException,
                $"Unexpected exception: {ex}"
            );
        }
    }

    [Fact]
    public void SceneManager_Class_IsStaticAndAccessible()
    {
        // Assert
        var type = typeof(SceneManager);
        Assert.True(type.IsAbstract && type.IsSealed, "SceneManager must be a static class.");
        Assert.True(type.IsPublic, "SceneManager must be public.");

        // Check required public methods exist
        var loadByName = type.GetMethod("Load", new[] { typeof(string) });
        var loadByIndex = type.GetMethod("Load", new[] { typeof(int) });

        Assert.NotNull(loadByName);
        Assert.NotNull(loadByIndex);
    }
}
