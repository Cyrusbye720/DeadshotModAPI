using System;
using DeadshotModAPI.Tests.TestHelpers;
using Xunit;

namespace DeadshotModAPI.Tests.Tier2_BoundaryCornerCases;

public class LoggerBoundaryTests : IDisposable
{
    public LoggerBoundaryTests()
    {
        TestLoggerMock.ResetLogs();
    }

    public void Dispose()
    {
        TestLoggerMock.ResetLogs();
    }

    [Fact]
    public void Logger_NullMessage_ExecutesDefensivelyAcrossAllLevels()
    {
        var exLog = Record.Exception(() => Logger.Log(null!));
        var exInfo = Record.Exception(() => Logger.Info(null!));
        var exWarn = Record.Exception(() => Logger.Warning(null!));
        var exErr = Record.Exception(() => Logger.Error(null!));
        var exDbg = Record.Exception(() => Logger.Debug(null!));

        Assert.Null(exLog);
        Assert.Null(exInfo);
        Assert.Null(exWarn);
        Assert.Null(exErr);
        Assert.Null(exDbg);
    }

    [Fact]
    public void Logger_EmptyAndWhitespaceStrings_PreservedAndLogged()
    {
        Logger.Info("");
        Logger.Warning("   ");
        Logger.Error("\t\r\n");

        Assert.Contains("", TestLoggerMock.LoggedInfo);
        Assert.Contains("   ", TestLoggerMock.LoggedWarnings);
        Assert.Contains("\t\r\n", TestLoggerMock.LoggedErrors);
    }

    [Fact]
    public void Logger_SpecialAndFormatCharacters_DoNotThrowFormatException()
    {
        string tricky = "Format string {0} and curly braces {} and %s %d \0 null-char";

        var ex = Record.Exception(() =>
        {
            Logger.Info(tricky);
            Logger.Warning(tricky);
            Logger.Error(tricky);
            Logger.Debug(tricky);
        });

        Assert.Null(ex);
        Assert.Contains(tricky, TestLoggerMock.LoggedInfo);
    }

    [Fact]
    public void Logger_ExtremelyLargeString_LogsWithoutBufferOverflow()
    {
        string largeString = new string('X', 100_000);

        var ex = Record.Exception(() => Logger.Info(largeString));

        Assert.Null(ex);
        Assert.Contains(largeString, TestLoggerMock.LoggedInfo);
    }

    [Fact]
    public void Logger_UnicodeAndEmojiCharacters_PreservedFaithfully()
    {
        string unicodeString = "Hello 🌍! Modding: 日本語, Español, Русский, 🎮🔥";

        Logger.Info(unicodeString);

        Assert.Contains(unicodeString, TestLoggerMock.LoggedInfo);
    }
}
