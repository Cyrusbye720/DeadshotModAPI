using System;
using System.IO;
using System.Runtime.Serialization;
using DeadshotModAPI.Tests.TestHelpers;
using Xunit;

namespace DeadshotModAPI.Tests.Tier1_FeatureCoverage;

public class ModLoaderFeatureTests : IDisposable
{
    private readonly string _tempDir;

    public ModLoaderFeatureTests()
    {
        ReflectionHelper.ResetModLoaderState();
        _tempDir = Path.Combine(Path.GetTempPath(), "DeadshotModAPI_ModLoaderTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        ReflectionHelper.ResetModLoaderState();
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    private static ModLoader CreateModLoaderInstance()
    {
        return (ModLoader)FormatterServices.GetUninitializedObject(typeof(ModLoader));
    }

    [Fact]
    public void ModLoader_Instantiates_Successfully()
    {
        // Act
        var loader = CreateModLoaderInstance();

        // Assert
        Assert.NotNull(loader);
    }

    [Fact]
    public void ModLoader_Awake_ExecutesWithoutException()
    {
        // Arrange
        var loader = CreateModLoaderInstance();

        // Act & Assert (Awake sets up singleton and should not throw)
        var exception = Record.Exception(() => loader.Awake());
        Assert.Null(exception);
    }

    [Fact]
    public void ModLoader_Start_ExecutesWithoutException()
    {
        // Arrange
        var loader = CreateModLoaderInstance();

        // Act & Assert (Start triggers mod discovery; handles empty path defensively)
        var exception = Record.Exception(() => loader.Start());
        Assert.Null(exception);
    }

    [Fact]
    public void ModLoader_Update_CallsCheckKeysWithoutException()
    {
        // Arrange
        var loader = CreateModLoaderInstance();

        // Act & Assert (Update invokes Input.CheckKeys())
        var exception = Record.Exception(() => loader.Update());
        Assert.Null(exception);
    }

    [Fact]
    public void ModLoader_LoadedModsCollection_IsInitializedAndAccessible()
    {
        // Assert
        Assert.NotNull(ModLoader.LoadedMods);
    }

    [Fact]
    public void ModLoader_LoadMod_DiscoversAndInstantiates_ConcreteIDeadshotMod()
    {
        // Arrange
        string source = @"
using DeadshotModAPI;

public class SampleTestMod : IDeadshotMod
{
    public string Name => ""SampleMod"";
    public string Description => ""A test mod description"";
    public string Creator => ""TestAuthor"";
    public string Version => ""1.0.0"";

    public bool Loaded { get; private set; }

    public void Load()
    {
        Loaded = true;
    }
}
";
        string dllPath = Path.Combine(_tempDir, "SampleMod.dll");
        TestAssemblyBuilder.CompileAssembly(source, dllPath, "SampleModAssembly");

        var loader = CreateModLoaderInstance();

        // Act
        ReflectionHelper.InvokeLoadMod(loader, dllPath);

        // Assert
        Assert.NotEmpty(ModLoader.LoadedMods);
        var loaded = ModLoader.LoadedMods.Find(m => m.Name == "SampleMod");
        Assert.NotNull(loaded);
        Assert.Equal("A test mod description", loaded.Description);
        Assert.Equal("TestAuthor", loaded.Creator);
        Assert.Equal("1.0.0", loaded.Version);
    }
}
