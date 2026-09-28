using System;
using System.IO;
using System.Runtime.Serialization;
using DeadshotModAPI.Tests.TestHelpers;
using Xunit;

namespace DeadshotModAPI.Tests.Tier2_BoundaryCornerCases;

public class DeadshotModBoundaryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ModLoader _loader;

    public DeadshotModBoundaryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "Deadshot_Mod_Boundary_" + Guid.NewGuid().ToString("N"));
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

    private class NullMetadataMod : IDeadshotMod
    {
        public string Name => null!;
        public string Description => null!;
        public string Creator => null!;
        public string Version => null!;
        public bool Loaded { get; private set; }
        public void Load() => Loaded = true;
    }

    [Fact]
    public void DeadshotMod_NullMetadata_DoesNotThrowOnInterfaceAccess()
    {
        IDeadshotMod mod = new NullMetadataMod();

        Assert.Null(mod.Name);
        Assert.Null(mod.Description);
        Assert.Null(mod.Creator);
        Assert.Null(mod.Version);

        mod.Load();
        Assert.True(((NullMetadataMod)mod).Loaded);
    }

    private class EmptyMetadataMod : IDeadshotMod
    {
        public string Name => "";
        public string Description => "   ";
        public string Creator => "\t";
        public string Version => "\n";
        public void Load() { }
    }

    [Fact]
    public void DeadshotMod_EmptyOrWhitespaceMetadata_PreservedWithoutException()
    {
        IDeadshotMod mod = new EmptyMetadataMod();

        Assert.Equal("", mod.Name);
        Assert.Equal("   ", mod.Description);
        Assert.Equal("\t", mod.Creator);
        Assert.Equal("\n", mod.Version);
    }

    [Fact]
    public void DeadshotMod_ThrowingPropertyGetters_HandledGracefullyByModLoader()
    {
        string code = @"
using System;
using DeadshotModAPI;
public class ThrowingPropsMod : IDeadshotMod
{
    public string Name => throw new InvalidOperationException(""Name exploded!"");
    public string Description => throw new InvalidOperationException(""Desc exploded!"");
    public string Creator => throw new InvalidOperationException(""Creator exploded!"");
    public string Version => throw new InvalidOperationException(""Version exploded!"");
    public bool DidLoad = false;
    public void Load() { DidLoad = true; }
}";
        string dllPath = Path.Combine(_tempDir, "ThrowingPropsMod.dll");
        TestAssemblyBuilder.CompileAssembly(code, dllPath, "ThrowingPropsAssembly");

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, dllPath));

        Assert.Null(ex);
        Assert.Single(ModLoader.LoadedMods);
        Assert.Contains(TestLoggerMock.LoggedWarnings, w => w.Contains("Failed to read mod name"));
    }

    [Fact]
    public void DeadshotMod_MultipleInstancesOfModClass_CanBeInstantiatedIndependently()
    {
        var mod1 = new NullMetadataMod();
        var mod2 = new NullMetadataMod();

        mod1.Load();

        Assert.True(mod1.Loaded);
        Assert.False(mod2.Loaded);
    }

    [Fact]
    public void DeadshotMod_UnicodeAndSpecialMetadata_PreservedAccurately()
    {
        string code = @"
using DeadshotModAPI;
public class UnicodeMetadataMod : IDeadshotMod
{
    public string Name => ""Mod 🚀 日本語"";
    public string Description => ""Desc with emojis 🎮"";
    public string Creator => ""Author: René / José"";
    public string Version => ""2.0.0-beta.1+build.123"";
    public void Load() { }
}";
        string dllPath = Path.Combine(_tempDir, "UnicodeMetadataMod.dll");
        TestAssemblyBuilder.CompileAssembly(code, dllPath, "UnicodeMetadataAssembly");

        ReflectionHelper.InvokeLoadMod(_loader, dllPath);

        Assert.Single(ModLoader.LoadedMods);
        var loaded = ModLoader.LoadedMods[0];
        Assert.Equal("Mod 🚀 日本語", loaded.Name);
        Assert.Equal("2.0.0-beta.1+build.123", loaded.Version);
    }
}
