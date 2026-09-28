using System;
using System.IO;
using System.Runtime.Serialization;
using DeadshotModAPI.Tests.TestHelpers;
using Xunit;

namespace DeadshotModAPI.Tests.Tier2_BoundaryCornerCases;

public class ModLoaderBoundaryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ModLoader _loader;

    public ModLoaderBoundaryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "Deadshot_ML_Boundary_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _loader = (ModLoader)FormatterServices.GetUninitializedObject(typeof(ModLoader));
        ReflectionHelper.ResetModLoaderState();
        TestLoggerMock.ResetLogs();
    }

    public void Dispose()
    {
        ReflectionHelper.ResetModLoaderState();
        TestLoggerMock.ResetLogs();
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    [Fact]
    public void ModLoader_LoadMod_NullPath_LogsWarningAndDoesNotThrow()
    {
        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, null!));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedWarnings, w => w.Contains("null or empty"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void ModLoader_LoadMod_WhitespacePath_LogsWarningAndDoesNotThrow(string whitespacePath)
    {
        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, whitespacePath));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedWarnings, w => w.Contains("null or empty"));
    }

    [Fact]
    public void ModLoader_LoadMod_NonexistentFile_LogsErrorAndDoesNotThrow()
    {
        string fakePath = Path.Combine(_tempDir, "DoesNotExist_12345.dll");
        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, fakePath));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("does not exist"));
    }

    [Fact]
    public void ModLoader_LoadMod_ZeroByteDll_LogsErrorAndDoesNotThrow()
    {
        string zeroByteDll = Path.Combine(_tempDir, "ZeroByte.dll");
        File.WriteAllBytes(zeroByteDll, Array.Empty<byte>());

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, zeroByteDll));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Corrupt or invalid mod assembly"));
    }

    [Fact]
    public void ModLoader_LoadMod_CorruptDllBytes_LogsErrorAndDoesNotThrow()
    {
        string corruptDll = Path.Combine(_tempDir, "Corrupted.dll");
        File.WriteAllBytes(corruptDll, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x11, 0x22, 0x33 });

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, corruptDll));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Corrupt or invalid mod assembly"));
    }

    [Fact]
    public void ModLoader_LoadMod_AbstractClassImplementingInterface_IsIgnored()
    {
        string code = @"
using DeadshotModAPI;
public abstract class AbstractTestMod : IDeadshotMod
{
    public string Name => ""AbstractMod"";
    public string Description => ""Desc"";
    public string Creator => ""Author"";
    public string Version => ""1.0"";
    public abstract void Load();
}";
        string dllPath = Path.Combine(_tempDir, "AbstractMod.dll");
        TestAssemblyBuilder.CompileAssembly(code, dllPath, "AbstractModAssembly");

        ReflectionHelper.InvokeLoadMod(_loader, dllPath);

        Assert.Empty(ModLoader.LoadedMods);
    }

    [Fact]
    public void ModLoader_LoadMod_ThrowingLoadMethod_CatchesExceptionAndLogsError()
    {
        string code = @"
using System;
using DeadshotModAPI;
public class CrashOnLoadMod : IDeadshotMod
{
    public string Name => ""CrashMod"";
    public string Description => ""Desc"";
    public string Creator => ""Author"";
    public string Version => ""1.0"";
    public void Load() => throw new ApplicationException(""Crashing during load!"");
}";
        string dllPath = Path.Combine(_tempDir, "CrashOnLoadMod.dll");
        TestAssemblyBuilder.CompileAssembly(code, dllPath, "CrashOnLoadModAssembly");

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, dllPath));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Crashing during load"));
    }
}
