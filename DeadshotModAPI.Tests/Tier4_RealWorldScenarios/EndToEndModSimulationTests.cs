using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using DeadshotModAPI.Tests.TestHelpers;
using Xunit;

namespace DeadshotModAPI.Tests.Tier4_RealWorldScenarios;

public class EndToEndModSimulationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ModLoader _loader;

    public EndToEndModSimulationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "Deadshot_Tier4_" + Guid.NewGuid().ToString("N"));
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
    public void Scenario1_FullProductionModLifecycle_RegistersHotkeys_TriggersSubsystems_TracksState()
    {
        string cheatModSource = @"
using System;
using DeadshotModAPI;

public class DeadshotCheatMenuMod : IDeadshotMod
{
    public string Name => ""Deadshot Cheat Menu"";
    public string Description => ""In-game trainer for Deadshot: toggles godmode, teleport, and restart."";
    public string Creator => ""ModderDev"";
    public string Version => ""1.2.0"";

    public static bool GodMode = false;
    public static int TeleportCount = 0;
    public static int RestartCount = 0;

    public void Load()
    {
        Logger.Info(""Initializing Deadshot Cheat Menu v1.2.0..."");
        Input.OnKeyPressed(Key.F8, OnToggleGodMode);
        Input.OnKeyPressed(Key.F9, OnTeleport);
        Input.OnKeyPressed(Key.F10, OnQuickRestart);
        Logger.Info(""Deadshot Cheat Menu hotkeys registered: F8 (GodMode), F9 (Teleport), F10 (Restart)."");
    }

    private void OnToggleGodMode()
    {
        GodMode = !GodMode;
        Logger.Info($""GodMode toggled: {GodMode}"");
    }

    private void OnTeleport()
    {
        TeleportCount++;
        SceneManager.Load(""Level_03_BossArena"");
    }

    private void OnQuickRestart()
    {
        RestartCount++;
        GameManager.RestartLevel(false);
    }
}";
        string dllPath = Path.Combine(_tempDir, "DeadshotCheatMenu.dll");
        TestAssemblyBuilder.CompileAssembly(cheatModSource, dllPath, "CheatMenuAssembly");

        // 1. ModLoader discovers and loads mod
        ReflectionHelper.InvokeLoadMod(_loader, dllPath);

        // Verify mod registration
        Assert.Single(ModLoader.LoadedMods);
        var modInstance = ModLoader.LoadedMods[0];
        Assert.Equal("Deadshot Cheat Menu", modInstance.Name);
        Assert.Equal("1.2.0", modInstance.Version);
        Assert.Contains(TestLoggerMock.LoggedInfo, msg => msg.Contains("Initializing Deadshot Cheat Menu"));

        var modType = modInstance.GetType();
        var godModeField = modType.GetField("GodMode", BindingFlags.Public | BindingFlags.Static)!;
        var teleportField = modType.GetField("TeleportCount", BindingFlags.Public | BindingFlags.Static)!;
        var restartField = modType.GetField("RestartCount", BindingFlags.Public | BindingFlags.Static)!;

        // 2. Simulate player pressing F8 (Toggle GodMode to True)
        ReflectionHelper.InvokeTriggerKey(Key.F8);
        Assert.True((bool)godModeField.GetValue(null)!);
        Assert.Contains(TestLoggerMock.LoggedInfo, msg => msg.Contains("GodMode toggled: True"));

        // 3. Simulate player pressing F8 again (Toggle GodMode to False)
        ReflectionHelper.InvokeTriggerKey(Key.F8);
        Assert.False((bool)godModeField.GetValue(null)!);
        Assert.Contains(TestLoggerMock.LoggedInfo, msg => msg.Contains("GodMode toggled: False"));

        // 4. Simulate player pressing F9 (Teleport -> SceneManager.Load)
        ReflectionHelper.InvokeTriggerKey(Key.F9);
        Assert.Equal(1, (int)teleportField.GetValue(null)!);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Failed to load scene 'Level_03_BossArena'"));

        // 5. Simulate player pressing F10 (Quick Restart -> GameManager.RestartLevel)
        ReflectionHelper.InvokeTriggerKey(Key.F10);
        Assert.Equal(1, (int)restartField.GetValue(null)!);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Failed to restart level"));
    }

    [Fact]
    public void Scenario2_MultiModEcosystem_CoexistingModsWithSharedState()
    {
        string coreSource = @"
using DeadshotModAPI;
public class CoreLibMod : IDeadshotMod
{
    public string Name => ""CoreLib"";
    public string Description => ""Provides shared state"";
    public string Creator => ""Team"";
    public string Version => ""1.0"";
    public static int SharedCounter = 0;

    public void Load()
    {
        Input.OnKeyPressed(Key.F1, () => SharedCounter += 10);
    }
}";
        string featureSource = @"
using DeadshotModAPI;
public class FeatureMod : IDeadshotMod
{
    public string Name => ""FeatureMod"";
    public string Description => ""Uses shared state"";
    public string Creator => ""Team"";
    public string Version => ""1.0"";
    public static int FeatureCounter = 0;

    public void Load()
    {
        Input.OnKeyPressed(Key.F2, () => FeatureCounter += 1);
    }
}";
        string coreDll = Path.Combine(_tempDir, "01_CoreLib.dll");
        string featureDll = Path.Combine(_tempDir, "02_Feature.dll");

        TestAssemblyBuilder.CompileAssembly(coreSource, coreDll, "CoreLibAssembly");
        TestAssemblyBuilder.CompileAssembly(featureSource, featureDll, "FeatureAssembly");

        ReflectionHelper.InvokeLoadMod(_loader, coreDll);
        ReflectionHelper.InvokeLoadMod(_loader, featureDll);

        Assert.Equal(2, ModLoader.LoadedMods.Count);

        var coreMod = ModLoader.LoadedMods.Find(m => m.Name == "CoreLib")!;
        var featMod = ModLoader.LoadedMods.Find(m => m.Name == "FeatureMod")!;

        var sharedField = coreMod.GetType().GetField("SharedCounter", BindingFlags.Public | BindingFlags.Static)!;
        var featField = featMod.GetType().GetField("FeatureCounter", BindingFlags.Public | BindingFlags.Static)!;

        // Trigger F1 twice
        ReflectionHelper.InvokeTriggerKey(Key.F1);
        ReflectionHelper.InvokeTriggerKey(Key.F1);

        // Trigger F2 three times
        ReflectionHelper.InvokeTriggerKey(Key.F2);
        ReflectionHelper.InvokeTriggerKey(Key.F2);
        ReflectionHelper.InvokeTriggerKey(Key.F2);

        Assert.Equal(20, (int)sharedField.GetValue(null)!);
        Assert.Equal(3, (int)featField.GetValue(null)!);
    }

    [Fact]
    public void Scenario3_ResilientProductionEnvironment_FaultyModsDoNotImpactHealthyMods()
    {
        // 1. Healthy Mod 1
        string good1Source = @"
using DeadshotModAPI;
public class GoodMod1 : IDeadshotMod
{
    public string Name => ""GoodMod1"";
    public string Description => ""D"";
    public string Creator => ""C"";
    public string Version => ""1.0"";
    public static bool Triggered = false;
    public void Load() { Input.OnKeyPressed(Key.Home, () => Triggered = true); }
}";
        string good1Dll = Path.Combine(_tempDir, "Mod1_Good.dll");
        TestAssemblyBuilder.CompileAssembly(good1Source, good1Dll, "Good1Assembly");

        // 2. Corrupt DLL (invalid header)
        string corruptDll = Path.Combine(_tempDir, "Mod2_Corrupt.dll");
        File.WriteAllBytes(corruptDll, new byte[] { 0x00, 0x01, 0x02, 0x03 });

        // 3. Crashing Mod (throws in Load)
        string crashSource = @"
using System;
using DeadshotModAPI;
public class BadMod : IDeadshotMod
{
    public string Name => ""BadMod"";
    public string Description => ""D"";
    public string Creator => ""C"";
    public string Version => ""1.0"";
    public void Load() => throw new InvalidOperationException(""Critical initialization fault"");
}";
        string crashDll = Path.Combine(_tempDir, "Mod3_Crash.dll");
        TestAssemblyBuilder.CompileAssembly(crashSource, crashDll, "BadAssembly");

        // 4. Healthy Mod 2
        string good2Source = @"
using DeadshotModAPI;
public class GoodMod2 : IDeadshotMod
{
    public string Name => ""GoodMod2"";
    public string Description => ""D"";
    public string Creator => ""C"";
    public string Version => ""1.0"";
    public static bool Triggered = false;
    public void Load() { Input.OnKeyPressed(Key.End, () => Triggered = true); }
}";
        string good2Dll = Path.Combine(_tempDir, "Mod4_Good.dll");
        TestAssemblyBuilder.CompileAssembly(good2Source, good2Dll, "Good2Assembly");

        // Load all 4 in order
        ReflectionHelper.InvokeLoadMod(_loader, good1Dll);
        ReflectionHelper.InvokeLoadMod(_loader, corruptDll);
        ReflectionHelper.InvokeLoadMod(_loader, crashDll);
        ReflectionHelper.InvokeLoadMod(_loader, good2Dll);

        // Verify only GoodMod1 and GoodMod2 are loaded
        Assert.Equal(2, ModLoader.LoadedMods.Count);
        var loadedGood1 = ModLoader.LoadedMods.Find(m => m.Name == "GoodMod1")!;
        var loadedGood2 = ModLoader.LoadedMods.Find(m => m.Name == "GoodMod2")!;
        Assert.NotNull(loadedGood1);
        Assert.NotNull(loadedGood2);

        // Verify key triggers operate properly on surviving mods
        var flag1 = loadedGood1.GetType().GetField("Triggered", BindingFlags.Public | BindingFlags.Static)!;
        var flag2 = loadedGood2.GetType().GetField("Triggered", BindingFlags.Public | BindingFlags.Static)!;

        ReflectionHelper.InvokeTriggerKey(Key.Home);
        ReflectionHelper.InvokeTriggerKey(Key.End);

        Assert.True((bool)flag1.GetValue(null)!);
        Assert.True((bool)flag2.GetValue(null)!);

        // Verify errors were logged for faulty mods
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Corrupt or invalid mod assembly"));
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Critical initialization fault"));
    }

    [Fact]
    public void Scenario4_DynamicKeyRebinding_UnregistersOldKeyAndRegistersNewKeyAtRuntime()
    {
        int actionCount = 0;
        Action action = () => actionCount++;

        // Default binding: F5
        Key currentKey = Key.F5;
        Input.OnKeyPressed(currentKey, action);

        // Trigger F5
        ReflectionHelper.InvokeTriggerKey(Key.F5);
        Assert.Equal(1, actionCount);

        // Runtime rebind: user changes key from F5 to F6
        Input.RemoveKeyPressed(currentKey, action);
        currentKey = Key.F6;
        Input.OnKeyPressed(currentKey, action);

        // Pressing F5 now should do nothing
        ReflectionHelper.InvokeTriggerKey(Key.F5);
        Assert.Equal(1, actionCount);

        // Pressing F6 now triggers the action
        ReflectionHelper.InvokeTriggerKey(Key.F6);
        Assert.Equal(2, actionCount);
    }

    [Fact]
    public void Scenario5_ModUnloadCleanup_RemovingAllListenersLeavesCleanState()
    {
        bool callback1Fired = false;
        bool callback2Fired = false;

        Action action1 = () => callback1Fired = true;
        Action action2 = () => callback2Fired = true;

        Input.OnKeyPressed(Key.PageUp, action1);
        Input.OnKeyPressed(Key.PageDown, action2);

        // Remove both
        Input.RemoveKeyPressed(Key.PageUp, action1);
        Input.RemoveKeyPressed(Key.PageDown, action2);

        // Trigger both
        ReflectionHelper.InvokeTriggerKey(Key.PageUp);
        ReflectionHelper.InvokeTriggerKey(Key.PageDown);

        Assert.False(callback1Fired);
        Assert.False(callback2Fired);
    }
}
