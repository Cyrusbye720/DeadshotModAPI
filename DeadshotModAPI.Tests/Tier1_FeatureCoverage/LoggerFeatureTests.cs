using System;
using System.Reflection;
using DeadshotModAPI.Tests.TestHelpers;
using Xunit;

namespace DeadshotModAPI.Tests.Tier1_FeatureCoverage;

public class LoggerFeatureTests : IDisposable
{
    public LoggerFeatureTests()
    {
        TestLoggerMock.Initialize();
        TestLoggerMock.ResetLogs();
    }

    public void Dispose()
    {
        TestLoggerMock.ResetLogs();
    }

    [Fact]
    public void Logger_Info_ExecutesWithoutExceptionAndCapturesMessage()
    {
        Logger.Info("Test informational message");

        Assert.Contains("Test informational message", TestLoggerMock.LoggedInfo);
    }

    [Fact]
    public void Logger_Warning_ExecutesWithoutExceptionAndCapturesMessage()
    {
        Logger.Warning("Test warning message");

        Assert.Contains("Test warning message", TestLoggerMock.LoggedWarnings);
    }

    [Fact]
    public void Logger_Error_ExecutesWithoutExceptionAndCapturesMessage()
    {
        Logger.Error("Test error message");

        Assert.Contains("Test error message", TestLoggerMock.LoggedErrors);
    }

    [Fact]
    public void Logger_Debug_ExecutesWithoutExceptionAndCapturesMessage()
    {
        Logger.Debug("Test debug message");

        Assert.Contains("Test debug message", TestLoggerMock.LoggedDebug);
    }

    [Fact]
    public void Logger_Log_Alias_ExecutesWithoutExceptionAndCapturesMessage()
    {
        Logger.Log("Test standard log alias message");

        Assert.Contains("Test standard log alias message", TestLoggerMock.LoggedInfo);
    }

    [Fact]
    public void Logger_Class_IsStaticAndHasExpectedMethods()
    {
        var type = typeof(Logger);
        Assert.True(type.IsAbstract && type.IsSealed, "Logger must be a static class.");
        Assert.True(type.IsPublic, "Logger must be public.");

        var logMethod = type.GetMethod("Log", new[] { typeof(string) });
        var infoMethod = type.GetMethod("Info", new[] { typeof(string) });
        var warnMethod = type.GetMethod("Warning", new[] { typeof(string) });
        var errorMethod = type.GetMethod("Error", new[] { typeof(string) });
        var debugMethod = type.GetMethod("Debug", new[] { typeof(string) });

        Assert.NotNull(logMethod);
        Assert.NotNull(infoMethod);
        Assert.NotNull(warnMethod);
        Assert.NotNull(errorMethod);
        Assert.NotNull(debugMethod);
    }
}
