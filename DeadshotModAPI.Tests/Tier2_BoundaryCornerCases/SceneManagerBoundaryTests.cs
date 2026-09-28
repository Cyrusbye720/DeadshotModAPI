using System;
using DeadshotModAPI.Tests.TestHelpers;
using Xunit;

namespace DeadshotModAPI.Tests.Tier2_BoundaryCornerCases;

public class SceneManagerBoundaryTests : IDisposable
{
    public SceneManagerBoundaryTests()
    {
        TestLoggerMock.ResetLogs();
        TestLoggerMock.OverrideBepInExRootPath = null;
    }

    public void Dispose()
    {
        TestLoggerMock.ResetLogs();
        TestLoggerMock.OverrideBepInExRootPath = null;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    public void SceneManager_LoadString_NullOrWhitespace_LogsWarningAndDoesNotThrow(string sceneName)
    {
        var ex = Record.Exception(() => SceneManager.Load(sceneName));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedWarnings, w => w.Contains("null, empty, or whitespace"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void SceneManager_LoadInt_NegativeIndex_LogsWarningAndDoesNotThrow(int negativeIndex)
    {
        var ex = Record.Exception(() => SceneManager.Load(negativeIndex));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedWarnings, w => w.Contains("non-negative"));
    }

    [Fact]
    public void SceneManager_LoadString_NonexistentScene_CatchesExceptionAndLogsError()
    {
        var ex = Record.Exception(() => SceneManager.Load("UnknownScene_XYZ"));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Failed to load scene 'UnknownScene_XYZ'"));
    }

    [Fact]
    public void SceneManager_LoadInt_IndexWhenUnderlyingSystemThrows_CatchesAndLogsError()
    {
        var ex = Record.Exception(() => SceneManager.Load(9999));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Failed to load scene at index 9999"));
    }

    [Fact]
    public void SceneManager_LoadModsBundle_EmptyBepInExRoot_LogsErrorAndDoesNotThrow()
    {
        TestLoggerMock.OverrideBepInExRootPath = "";

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadModsBundle());

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("BepInEx root path is null or empty"));
    }

    [Fact]
    public void SceneManager_LoadModsBundle_MissingBundleFile_LogsErrorAndDoesNotThrow()
    {
        TestLoggerMock.OverrideBepInExRootPath = @"C:\NonExistentPath_TestBundle";

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadModsBundle());

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Mod bundle file does not exist"));
    }
}
