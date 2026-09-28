using System;
using System.Reflection;
using Xunit;

namespace DeadshotModAPI.Tests.Tier1_FeatureCoverage;

public class GameManagerFeatureTests
{
    [Fact]
    public void GameManager_RestartLevel_DefaultParameter_ExecutesDefensivelyWithoutThrowing()
    {
        // Act & Assert
        // In unit test environment, Unity GameManager singletons are uninitialized,
        // but RestartLevel wraps execution defensively with null checks and try-catch.
        var ex = Record.Exception(() => GameManager.RestartLevel());
        Assert.Null(ex);
    }

    [Fact]
    public void GameManager_RestartLevel_ExplicitPlayCutsceneFalse_ExecutesDefensivelyWithoutThrowing()
    {
        // Act & Assert
        var ex = Record.Exception(() => GameManager.RestartLevel(false));
        Assert.Null(ex);
    }

    [Fact]
    public void GameManager_RestartLevel_ExplicitPlayCutsceneTrue_ExecutesDefensivelyWithoutThrowing()
    {
        // Act & Assert
        var ex = Record.Exception(() => GameManager.RestartLevel(true));
        Assert.Null(ex);
    }

    [Fact]
    public void GameManager_Class_IsPublicAndHasExpectedMethods()
    {
        var type = typeof(GameManager);
        Assert.True(type.IsPublic);

        var restartMethod = type.GetMethod("RestartLevel", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(restartMethod);

        var parameters = restartMethod.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(bool), parameters[0].ParameterType);
        Assert.True(parameters[0].HasDefaultValue);
        Assert.Equal(true, parameters[0].DefaultValue);
    }

    [Fact]
    public void GameManager_Constants_VerifyInternalKeys()
    {
        var type = typeof(GameManager);

        var keyField = type.GetField("gameManagerKey", BindingFlags.NonPublic | BindingFlags.Static);
        var idField = type.GetField("gameManagerId", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(keyField);
        Assert.NotNull(idField);

        Assert.Equal("7069791d-77d0-45d8-8895-91b9fae46892", keyField.GetValue(null));
        Assert.Equal("Deadshot.GameManager", idField.GetValue(null));
    }
}
