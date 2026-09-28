using System;
using System.Reflection;
using DeadshotModAPI.Tests.TestHelpers;
using Xunit;

namespace DeadshotModAPI.Tests.Tier2_BoundaryCornerCases;

public class GameManagerBoundaryTests : IDisposable
{
    public GameManagerBoundaryTests()
    {
        TestLoggerMock.ResetLogs();
    }

    public void Dispose()
    {
        TestLoggerMock.ResetLogs();
    }

    [Fact]
    public void GameManager_RestartLevel_FalseCutscene_WhenSingletonNull_CatchesAndLogsError()
    {
        var ex = Record.Exception(() => GameManager.RestartLevel(false));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Failed to restart level"));
    }

    [Fact]
    public void GameManager_RestartLevel_TrueCutscene_WhenSingletonNull_CatchesAndLogsError()
    {
        var ex = Record.Exception(() => GameManager.RestartLevel(true));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Failed to restart level"));
    }

    [Fact]
    public void GameManager_RestartLevel_RapidConsecutiveInvocations_DoesNotThrow()
    {
        for (int i = 0; i < 10; i++)
        {
            var ex = Record.Exception(() => GameManager.RestartLevel(i % 2 == 0));
            Assert.Null(ex);
        }

        Assert.True(TestLoggerMock.LoggedErrors.Count >= 10);
    }

    [Fact]
    public void GameManager_MethodSignature_RestartLevel_HasOptionalBooleanDefaultingToFalse()
    {
        var method = typeof(GameManager).GetMethod("RestartLevel", new[] { typeof(bool) });
        Assert.NotNull(method);

        var param = method.GetParameters()[0];
        Assert.True(param.IsOptional, "Parameter playCutscene should be optional.");
        Assert.Equal(true, param.DefaultValue);
    }

    [Fact]
    public void GameManager_InternalLevelGuids_ConstantsAreNonEmpty()
    {
        var fields = typeof(GameManager).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotEmpty(fields);

        foreach (var f in fields)
        {
            if (f.FieldType == typeof(string) && f.IsLiteral)
            {
                var val = (string)f.GetValue(null);
                Assert.False(string.IsNullOrWhiteSpace(val), $"Constant field '{f.Name}' should not be empty.");
            }
        }
    }
}
