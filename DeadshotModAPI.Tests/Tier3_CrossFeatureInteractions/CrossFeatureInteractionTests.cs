using System;
using System.IO;
using System.Runtime.Serialization;
using DeadshotModAPI.Tests.TestHelpers;
using Xunit;

namespace DeadshotModAPI.Tests.Tier3_CrossFeatureInteractions;

public class CrossFeatureInteractionTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ModLoader _loader;

    public CrossFeatureInteractionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "Deadshot_Tier3_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _loader = (ModLoader)FormatterServices.GetUninitializedObject(typeof(ModLoader));
        ReflectionHelper.ResetInputState();
        ReflectionHelper.ResetModLoaderState();
        TestLoggerMock.ResetLogs();
    }

    public void Dispose()
    {
        ReflectionHelper.ResetInputState();
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
    public void Interaction1_KeyCallback_RegistersNewKeyInsideCallback_NewKeyFunctionsProperly()
    {
        bool secondaryFired = false;

        // Callback for F1 registers F2
        Input.OnKeyPressed(Key.F1, () =>
        {
            Input.OnKeyPressed(Key.F2, () => secondaryFired = true);
        });

        // Trigger F1
        ReflectionHelper.InvokeTriggerKey(Key.F1);
        Assert.False(secondaryFired, "F2 should not have fired yet.");

        // Now trigger F2
        ReflectionHelper.InvokeTriggerKey(Key.F2);
        Assert.True(secondaryFired, "F2 should fire now after dynamic registration.");
    }

    [Fact]
    public void Interaction2_KeyCallback_UnregistersSelfDuringCallback_SafelyCompletesInvocation()
    {
        int invocationCount = 0;
        Action callback = null!;

        callback = () =>
        {
            invocationCount++;
            Input.RemoveKeyPressed(Key.X, callback);
        };

        Input.OnKeyPressed(Key.X, callback);

        // First trigger: should run and remove itself
        ReflectionHelper.InvokeTriggerKey(Key.X);
        Assert.Equal(1, invocationCount);

        // Second trigger: should not run again
        ReflectionHelper.InvokeTriggerKey(Key.X);
        Assert.Equal(1, invocationCount);
    }

    [Fact]
    public void Interaction3_ModLoading_RegistersInputListener_WhichCanBeTriggered()
    {
        string modSource = @"
using DeadshotModAPI;
public class InputRegisteringMod : IDeadshotMod
{
    public string Name => ""InputRegMod"";
    public string Description => ""Registers hotkey"";
    public string Creator => ""Author"";
    public string Version => ""1.0"";
    public static bool KeyActionExecuted = false;

    public void Load()
    {
        Input.OnKeyPressed(Key.F7, OnF7Pressed);
    }

    private void OnF7Pressed()
    {
        KeyActionExecuted = true;
    }
}";
        string dllPath = Path.Combine(_tempDir, "InputRegMod.dll");
        TestAssemblyBuilder.CompileAssembly(modSource, dllPath, "InputRegModAssembly");

        // Load the mod through ModLoader
        ReflectionHelper.InvokeLoadMod(_loader, dllPath);

        Assert.Single(ModLoader.LoadedMods);
        var modInstance = ModLoader.LoadedMods[0];
        Assert.Equal("InputRegMod", modInstance.Name);

        // Verify key action was not yet executed
        var flagField = modInstance.GetType().GetField("KeyActionExecuted");
        Assert.NotNull(flagField);
        Assert.False((bool)flagField!.GetValue(null)!);

        // Trigger key
        ReflectionHelper.InvokeTriggerKey(Key.F7);

        // Verify key action executed through the mod
        Assert.True((bool)flagField.GetValue(null)!);
    }

    [Fact]
    public void Interaction4_MultipleModsInSingleDll_AllAreDiscoveredAndLoaded()
    {
        string multiModSource = @"
using DeadshotModAPI;
public class ModAlpha : IDeadshotMod
{
    public string Name => ""ModAlpha"";
    public string Description => ""First Mod"";
    public string Creator => ""Dev A"";
    public string Version => ""1.0"";
    public static bool Loaded = false;
    public void Load() { Loaded = true; }
}

public class ModBeta : IDeadshotMod
{
    public string Name => ""ModBeta"";
    public string Description => ""Second Mod"";
    public string Creator => ""Dev B"";
    public string Version => ""2.0"";
    public static bool Loaded = false;
    public void Load() { Loaded = true; }
}
";
        string dllPath = Path.Combine(_tempDir, "MultiMod.dll");
        TestAssemblyBuilder.CompileAssembly(multiModSource, dllPath, "MultiModAssembly");

        ReflectionHelper.InvokeLoadMod(_loader, dllPath);

        Assert.Equal(2, ModLoader.LoadedMods.Count);
        var alphaMod = ModLoader.LoadedMods.Find(m => m.Name == "ModAlpha");
        var betaMod = ModLoader.LoadedMods.Find(m => m.Name == "ModBeta");

        Assert.NotNull(alphaMod);
        Assert.NotNull(betaMod);

        var alphaFlag = alphaMod.GetType().GetField("Loaded");
        var betaFlag = betaMod.GetType().GetField("Loaded");
        Assert.True((bool)alphaFlag!.GetValue(null)!);
        Assert.True((bool)betaFlag!.GetValue(null)!);
    }

    [Fact]
    public void Interaction5_FailingModIsolation_HealthyModLoadsSuccessfullyAlongsideFailingMod()
    {
        string mixedSource = @"
using System;
using DeadshotModAPI;
public class GoodMod : IDeadshotMod
{
    public string Name => ""GoodMod"";
    public string Description => ""Works"";
    public string Creator => ""Dev"";
    public string Version => ""1.0"";
    public static bool Loaded = false;
    public void Load() { Loaded = true; }
}

public class FailingMod : IDeadshotMod
{
    public string Name => ""FailingMod"";
    public string Description => ""Fails"";
    public string Creator => ""Dev"";
    public string Version => ""1.0"";
    public void Load() => throw new ApplicationException(""Fatal mod initialization error"");
}
";
        string dllPath = Path.Combine(_tempDir, "MixedMod.dll");
        TestAssemblyBuilder.CompileAssembly(mixedSource, dllPath, "MixedModAssembly");

        ReflectionHelper.InvokeLoadMod(_loader, dllPath);

        // GoodMod should be successfully loaded
        Assert.Single(ModLoader.LoadedMods);
        var goodMod = ModLoader.LoadedMods[0];
        Assert.Equal("GoodMod", goodMod.Name);

        var goodFlag = goodMod.GetType().GetField("Loaded");
        Assert.True((bool)goodFlag!.GetValue(null)!);

        // FailingMod error should have been caught and logged
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Fatal mod initialization error"));
    }

    [Fact]
    public void Interaction6_KeyCallback_InvokingSceneManagerAndGameManager_SafelyExecutes()
    {
        bool callbackExecuted = false;

        Input.OnKeyPressed(Key.F11, () =>
        {
            callbackExecuted = true;
            // Mod hotkey invoking both SceneManager and GameManager
            SceneManager.Load("Arena_Level");
            GameManager.RestartLevel(true);
        });

        ReflectionHelper.InvokeTriggerKey(Key.F11);

        Assert.True(callbackExecuted);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Failed to load scene 'Arena_Level'"));
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Failed to restart level"));
    }
}
