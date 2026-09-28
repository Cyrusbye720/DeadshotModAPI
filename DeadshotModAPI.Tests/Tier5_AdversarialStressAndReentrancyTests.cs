using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using DeadshotModAPI.Tests.TestHelpers;
using Xunit;

namespace DeadshotModAPI.Tests;

/// <summary>
/// Tier 5 Adversarial Stress & Reentrancy Test Suite.
/// Authored by Challenger 2 for the Final Milestone (Adversarial Coverage Hardening).
/// Focus areas: Integration stress, concurrency, reentrancy cascades, memory cleanup (Unload/RemoveKeyPressed),
/// and corrupt filesystem states.
/// </summary>
public class Tier5_AdversarialStressAndReentrancyTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ModLoader _loader;

    public Tier5_AdversarialStressAndReentrancyTests()
    {
        TestLoggerMock.Initialize();
        TestLoggerMock.ResetLogs();
        ReflectionHelper.ResetInputState();
        ReflectionHelper.ResetModLoaderState();

        _tempDir = Path.Combine(Path.GetTempPath(), "Deadshot_Tier5_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _loader = (ModLoader)FormatterServices.GetUninitializedObject(typeof(ModLoader));
    }

    public void Dispose()
    {
        ReflectionHelper.ResetInputState();
        ReflectionHelper.ResetModLoaderState();
        TestLoggerMock.ResetLogs();
        TestLoggerMock.OverrideBepInExRootPath = null;

        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    private static Dictionary<Key, Action> GetInternalKeyActions()
    {
        var field = typeof(Input).GetField("_keyActions", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        return (Dictionary<Key, Action>)field!.GetValue(null)!;
    }

    #region Suite 1: ExampleMod Integration & Lifecycle Memory Cleanup

    [Fact]
    public void ExampleMod_FullLifecycle_Load_TriggerHotkeys_Unload_LeavesZeroLingeringCallbacks()
    {
        // 1. Locate and compile or load ExampleMod
        string exampleSource = @"
using System;
using DeadshotModAPI;

namespace ExampleMod;

public class ExampleMod : IDeadshotMod
{
    private bool _isCustomStateActive;
    public string Name => ""ExampleMod"";
    public string Description => ""Reference demonstration mod for DeadshotModAPI"";
    public string Creator => ""DemonZ-Development"";
    public string Version => ""1.0.0"";
    public bool IsCustomStateActive => _isCustomStateActive;

    public void Load()
    {
        Logger.Info($""[ExampleMod] Loading {Name} v{Version} by {Creator}..."");
        Input.OnKeyPressed(Key.F5, OnToggleCustomState);
        Input.OnKeyPressed(Key.F9, OnLoadSampleScene);
        Input.OnKeyPressed(Key.F10, OnRestartCurrentLevel);
        Logger.Info(""[ExampleMod] Loaded successfully."");
    }

    public void Unload()
    {
        Input.RemoveKeyPressed(Key.F5, OnToggleCustomState);
        Input.RemoveKeyPressed(Key.F9, OnLoadSampleScene);
        Input.RemoveKeyPressed(Key.F10, OnRestartCurrentLevel);
        Logger.Info(""[ExampleMod] Unloaded successfully."");
    }

    private void OnToggleCustomState()
    {
        _isCustomStateActive = !_isCustomStateActive;
        Logger.Info($""[ExampleMod] Custom mod state toggled: {_isCustomStateActive}"");
    }

    private void OnLoadSampleScene()
    {
        SceneManager.Load(""SampleScene"");
    }

    private void OnRestartCurrentLevel()
    {
        GameManager.RestartLevel(false);
    }
}
";
        string dllPath = Path.Combine(_tempDir, "ExampleMod_Dynamic.dll");
        TestAssemblyBuilder.CompileAssembly(exampleSource, dllPath, "ExampleModAssembly");

        // 2. Load mod via ModLoader
        ReflectionHelper.InvokeLoadMod(_loader, dllPath);

        Assert.Single(ModLoader.LoadedMods);
        var modInstance = ModLoader.LoadedMods[0];
        Assert.Equal("ExampleMod", modInstance.Name);
        Assert.Equal("1.0.0", modInstance.Version);
        Assert.Equal("DemonZ-Development", modInstance.Creator);

        var modType = modInstance.GetType();
        var stateProp = modType.GetProperty("IsCustomStateActive")!;
        Assert.False((bool)stateProp.GetValue(modInstance)!);

        var keyActions = GetInternalKeyActions();
        Assert.True(keyActions.ContainsKey(Key.F5));
        Assert.True(keyActions.ContainsKey(Key.F9));
        Assert.True(keyActions.ContainsKey(Key.F10));

        // 3. Trigger F5 -> Toggle state to true
        ReflectionHelper.InvokeTriggerKey(Key.F5);
        Assert.True((bool)stateProp.GetValue(modInstance)!);

        // Trigger F5 again -> Toggle state back to false
        ReflectionHelper.InvokeTriggerKey(Key.F5);
        Assert.False((bool)stateProp.GetValue(modInstance)!);

        // Trigger F9 and F10 -> Handled safely by SceneManager and GameManager
        ReflectionHelper.InvokeTriggerKey(Key.F9);
        ReflectionHelper.InvokeTriggerKey(Key.F10);

        // 4. Invoke Unload() on ExampleMod
        var unloadMethod = modType.GetMethod("Unload")!;
        Assert.NotNull(unloadMethod);
        unloadMethod.Invoke(modInstance, null);

        // 5. Verify all listeners have been purged from _keyActions
        Assert.False(keyActions.ContainsKey(Key.F5), "Key.F5 action should be completely removed from dictionary.");
        Assert.False(keyActions.ContainsKey(Key.F9), "Key.F9 action should be completely removed from dictionary.");
        Assert.False(keyActions.ContainsKey(Key.F10), "Key.F10 action should be completely removed from dictionary.");

        // 6. Trigger keys again to confirm mod no longer responds
        ReflectionHelper.InvokeTriggerKey(Key.F5);
        Assert.False((bool)stateProp.GetValue(modInstance)!, "State must not change after Unload().");

        // 7. Verify calling Unload() a second time is idempotent and safe
        var doubleUnloadEx = Record.Exception(() => unloadMethod.Invoke(modInstance, null));
        Assert.Null(doubleUnloadEx);
    }

    [Fact]
    public void ExampleMod_RapidConsecutiveLoadUnloadCycles_MaintainsDictionaryPurity()
    {
        string exampleSource = @"
using DeadshotModAPI;
public class CycleMod : IDeadshotMod
{
    public string Name => ""CycleMod"";
    public string Description => ""Cycles"";
    public string Creator => ""Author"";
    public string Version => ""1.0"";
    public void Load()
    {
        Input.OnKeyPressed(Key.F3, Handler);
        Input.OnKeyPressed(Key.F4, Handler);
    }
    public void Unload()
    {
        Input.RemoveKeyPressed(Key.F3, Handler);
        Input.RemoveKeyPressed(Key.F4, Handler);
    }
    private void Handler() { }
}
";
        string dllPath = Path.Combine(_tempDir, "CycleMod.dll");
        TestAssemblyBuilder.CompileAssembly(exampleSource, dllPath, "CycleModAssembly");

        var assembly = Assembly.LoadFrom(dllPath);
        var modType = assembly.GetType("CycleMod")!;
        var mod = (IDeadshotMod)Activator.CreateInstance(modType)!;
        var unloadMethod = modType.GetMethod("Unload")!;

        var keyActions = GetInternalKeyActions();

        for (int i = 0; i < 50; i++)
        {
            mod.Load();
            Assert.True(keyActions.ContainsKey(Key.F3));
            Assert.True(keyActions.ContainsKey(Key.F4));

            unloadMethod.Invoke(mod, null);
            Assert.False(keyActions.ContainsKey(Key.F3));
            Assert.False(keyActions.ContainsKey(Key.F4));
        }

        Assert.Empty(keyActions);
    }

    [Fact]
    public void MemoryCleanup_DeregisteredCallback_ReleasesModTargetForGarbageCollection()
    {
        WeakReference weakRef = RegisterAndDeregisterInstanceCallback();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weakRef.IsAlive, "Instance should have been garbage collected after callback deregistration.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RegisterAndDeregisterInstanceCallback()
    {
        var target = new ModInstanceTarget();
        Input.OnKeyPressed(Key.F1, target.Handler);
        var weakRef = new WeakReference(target);
        Input.RemoveKeyPressed(Key.F1, target.Handler);
        return weakRef;
    }

    private class ModInstanceTarget
    {
        public int Count { get; set; }
        public void Handler() => Count++;
    }

    [Fact]
    public void Input_RemoveKeyPressed_EdgeCases_DoNotThrowOrCorruptState()
    {
        // 1. Remove when dictionary is empty
        var ex1 = Record.Exception(() => Input.RemoveKeyPressed(Key.F12, () => { }));
        Assert.Null(ex1);

        // 2. Remove null delegate
        var ex2 = Record.Exception(() => Input.RemoveKeyPressed(Key.F12, null!));
        Assert.Null(ex2);

        // 3. Remove non-registered delegate from an existing key
        Input.OnKeyPressed(Key.F12, () => { });
        var ex3 = Record.Exception(() => Input.RemoveKeyPressed(Key.F12, () => { }));
        Assert.Null(ex3);

        var keyActions = GetInternalKeyActions();
        Assert.True(keyActions.ContainsKey(Key.F12));
    }

    #endregion

    #region Suite 2: Reentrancy & Nested Invocation Cascades

    [Fact]
    public void Reentrancy_DeepCascadingKeyInvocations_ExecutesInStrictHierarchicalOrder()
    {
        var executionLog = new List<string>();

        Input.OnKeyPressed(Key.A, () =>
        {
            executionLog.Add("Start A");
            ReflectionHelper.InvokeTriggerKey(Key.B);
            executionLog.Add("End A");
        });

        Input.OnKeyPressed(Key.B, () =>
        {
            executionLog.Add("Start B");
            ReflectionHelper.InvokeTriggerKey(Key.C);
            executionLog.Add("End B");
        });

        Input.OnKeyPressed(Key.C, () =>
        {
            executionLog.Add("Start C");
            ReflectionHelper.InvokeTriggerKey(Key.D);
            executionLog.Add("End C");
        });

        Input.OnKeyPressed(Key.D, () =>
        {
            executionLog.Add("Executing D");
        });

        var ex = Record.Exception(() => ReflectionHelper.InvokeTriggerKey(Key.A));

        Assert.Null(ex);
        Assert.Equal(new[]
        {
            "Start A",
            "Start B",
            "Start C",
            "Executing D",
            "End C",
            "End B",
            "End A"
        }, executionLog);
    }

    [Fact]
    public void Reentrancy_CallbackRemovesSelfAndAddsReplacementKey_ExecutesCorrectly()
    {
        int invocationCount = 0;
        bool replacementExecuted = false;
        Action callback = null!;

        callback = () =>
        {
            invocationCount++;
            Input.RemoveKeyPressed(Key.Q, callback);
            Input.OnKeyPressed(Key.W, () => replacementExecuted = true);
        };

        Input.OnKeyPressed(Key.Q, callback);

        // Trigger Q -> Unregisters Q, registers W
        ReflectionHelper.InvokeTriggerKey(Key.Q);
        Assert.Equal(1, invocationCount);
        Assert.False(replacementExecuted);

        // Trigger Q again -> Should not run
        ReflectionHelper.InvokeTriggerKey(Key.Q);
        Assert.Equal(1, invocationCount);

        // Trigger W -> Should run replacement
        ReflectionHelper.InvokeTriggerKey(Key.W);
        Assert.True(replacementExecuted);
    }

    [Fact]
    public void Reentrancy_NestedExceptionInCascade_DoesNotAbortOuterExecution()
    {
        bool outerStep1 = false;
        bool outerStep2 = false;

        Input.OnKeyPressed(Key.F6, () =>
        {
            outerStep1 = true;
            // Reentrantly trigger F7 which throws
            ReflectionHelper.InvokeTriggerKey(Key.F7);
            outerStep2 = true;
        });

        Input.OnKeyPressed(Key.F7, () =>
        {
            throw new InvalidOperationException("Cascaded child explosion");
        });

        var ex = Record.Exception(() => ReflectionHelper.InvokeTriggerKey(Key.F6));

        Assert.Null(ex);
        Assert.True(outerStep1);
        Assert.True(outerStep2, "Outer execution must resume and complete despite child cascade exception.");
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Cascaded child explosion"));
    }

    #endregion

    #region Suite 3: Concurrency & Multi-Threaded Stress

    [Fact]
    public async Task Concurrency_ParallelSceneManagerLoad_ExecutesSafelyWithoutDeadlock()
    {
        int taskCount = 40;
        var tasks = new Task[taskCount];

        for (int i = 0; i < taskCount; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                if (index % 2 == 0)
                    SceneManager.Load($"Scene_Stress_{index}");
                else
                    SceneManager.Load(index);
            });
        }

        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Concurrency_ParallelGameManagerRestartLevel_ExecutesSafelyWithoutDeadlock()
    {
        int taskCount = 30;
        var tasks = new Task[taskCount];

        for (int i = 0; i < taskCount; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                GameManager.RestartLevel(index % 2 == 0);
            });
        }

        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Concurrency_ParallelLogger_DispatchesSafelyAcrossAllLevels()
    {
        int taskCount = 40;
        var tasks = new Task[taskCount];

        for (int i = 0; i < taskCount; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                Logger.Info($"Parallel Info {index}");
                Logger.Warning($"Parallel Warning {index}");
                Logger.Error($"Parallel Error {index}");
                Logger.Debug($"Parallel Debug {index}");
                Logger.Log($"Parallel Log {index}");
            });
        }

        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotEmpty(TestLoggerMock.LoggedInfo);
        Assert.NotEmpty(TestLoggerMock.LoggedWarnings);
        Assert.NotEmpty(TestLoggerMock.LoggedErrors);
    }

    #endregion

    #region Suite 4: Rapid Repeated Invocations (Burst / Stress)

    [Fact]
    public void Stress_RapidTriggerKeyBurst_10000Invocations_ExecutesReliably()
    {
        int totalExecutions = 0;
        Input.OnKeyPressed(Key.T, () => totalExecutions++);

        for (int i = 0; i < 10000; i++)
        {
            ReflectionHelper.InvokeTriggerKey(Key.T);
        }

        Assert.Equal(10000, totalExecutions);
    }

    [Fact]
    public void Stress_RapidSceneManagerLoadBurst_1000Invocations_NoMemoryLeakOrCrash()
    {
        for (int i = 0; i < 1000; i++)
        {
            if (i % 3 == 0)
                SceneManager.Load((string)null!);
            else if (i % 3 == 1)
                SceneManager.Load(-i);
            else
                SceneManager.Load(i);
        }
    }

    [Fact]
    public void Stress_RapidGameManagerRestartLevelBurst_500Invocations_NoCrash()
    {
        for (int i = 0; i < 500; i++)
        {
            GameManager.RestartLevel(i % 2 == 0);
        }
    }

    #endregion

    #region Suite 5: Corrupt Filesystem States & Extreme ModLoader Stress

    [Fact]
    public void CorruptFilesystem_ModsFolderIsAFile_HandledDefensivelyWithoutCrash()
    {
        // Setup mock BepInEx root where "mods" is a regular file instead of a directory
        string fakeRoot = Path.Combine(_tempDir, "FakeBepInEx");
        Directory.CreateDirectory(fakeRoot);
        string modsFile = Path.Combine(fakeRoot, "mods");
        File.WriteAllText(modsFile, "This is a file blocking directory creation.");

        TestLoggerMock.OverrideBepInExRootPath = fakeRoot;

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMods(_loader));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err =>
            err.Contains("Failed to verify or create mod directory") ||
            err.Contains("Failed to enumerate mod files"));
    }

    [Fact]
    public void CorruptFilesystem_ExclusivelyLockedModDll_HandledDefensivelyWithoutCrash()
    {
        string lockedPath = Path.Combine(_tempDir, "LockedMod.dll");
        File.WriteAllBytes(lockedPath, new byte[] { 0x4D, 0x5A, 0x90, 0x00 }); // MZ header

        // Open with FileShare.None to simulate another process locking the file exclusively
        using var stream = new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, lockedPath));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err =>
            err.Contains("Failed to load assembly 'LockedMod.dll'"));
    }

    [Fact]
    public void CorruptFilesystem_DirectoryWithDllExtension_HandledDefensivelyWithoutCrash()
    {
        string fakeDllDir = Path.Combine(_tempDir, "FakeFolder.dll");
        Directory.CreateDirectory(fakeDllDir);

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, fakeDllDir));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err =>
            err.Contains("Mod file does not exist"));
    }

    [Fact]
    public void CorruptFilesystem_AssemblyWithGenericIDeadshotMod_CaughtAndLoggedDefensively()
    {
        string genericSource = @"
using DeadshotModAPI;
public class GenericMod<T> : IDeadshotMod
{
    public string Name => ""GenericMod"";
    public string Description => ""Generic"";
    public string Creator => ""Author"";
    public string Version => ""1.0"";
    public void Load() { }
}
";
        string dllPath = Path.Combine(_tempDir, "GenericModAssembly.dll");
        TestAssemblyBuilder.CompileAssembly(genericSource, dllPath, "GenericModAssembly");

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, dllPath));

        Assert.Null(ex);
        Assert.Empty(ModLoader.LoadedMods);
        Assert.Contains(TestLoggerMock.LoggedErrors, err =>
            err.Contains("Failed to instantiate or load mod 'GenericMod`1'"));
    }

    [Fact]
    public void CorruptFilesystem_AssemblyWithOnlyStaticClasses_DiscoversZeroModsWithoutError()
    {
        string staticSource = @"
public static class StaticUtilityClass
{
    public static int Add(int a, int b) => a + b;
}
public struct PointStruct
{
    public int X;
    public int Y;
}
";
        string dllPath = Path.Combine(_tempDir, "StaticAssembly.dll");
        TestAssemblyBuilder.CompileAssembly(staticSource, dllPath, "StaticAssembly");

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, dllPath));

        Assert.Null(ex);
        Assert.Empty(ModLoader.LoadedMods);
        Assert.Empty(TestLoggerMock.LoggedErrors);
    }

    #endregion
}
