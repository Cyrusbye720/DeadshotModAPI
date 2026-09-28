using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using DeadshotModAPI.Tests.TestHelpers;
using HarmonyLib;
using Xunit;

namespace DeadshotModAPI.Tests;

public class AdversarialM2Tests : IDisposable
{
    private readonly string _tempDir;
    private static Harmony _harmonyKeyboardMock;

    public AdversarialM2Tests()
    {
        TestLoggerMock.Initialize();
        TestLoggerMock.ResetLogs();
        ReflectionHelper.ResetInputState();
        ReflectionHelper.ResetModLoaderState();

        SetupKeyboardMock();

        _tempDir = Path.Combine(Path.GetTempPath(), "DeadshotModAPI_AdvM2_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    private static void SetupKeyboardMock()
    {
        if (_harmonyKeyboardMock != null) return;

        try
        {
            _harmonyKeyboardMock = new Harmony("DeadshotModAPI.Tests.KeyboardMock");
            var getCurrentMethod = typeof(UnityEngine.InputSystem.Keyboard).GetMethod("get_current", BindingFlags.Public | BindingFlags.Static);
            if (getCurrentMethod != null)
            {
                var prefix = typeof(AdversarialM2Tests).GetMethod(nameof(PrefixKeyboardCurrent), BindingFlags.NonPublic | BindingFlags.Static);
                _harmonyKeyboardMock.Patch(getCurrentMethod, new HarmonyMethod(prefix));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[KeyboardMock Warning] Could not patch Keyboard.get_current: {ex.Message}");
        }
    }

    private static bool PrefixKeyboardCurrent(ref UnityEngine.InputSystem.Keyboard __result)
    {
        __result = null;
        return false; // Skip original method
    }

    public void Dispose()
    {
        ReflectionHelper.ResetInputState();
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

    private static bool InvokeIsPressed(Key key)
    {
        var method = typeof(Input).GetMethod("IsPressed", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (bool)method!.Invoke(null, new object[] { key })!;
    }

    // =========================================================================
    // Edge Case 1: Keyboard.current is null when calling Input.IsPressed or Input.CheckKeys
    // =========================================================================

    [Fact]
    public void EdgeCase1_KeyboardNull_IsPressed_ReturnsFalseSafelyWithoutTargetException()
    {
        // When Keyboard.current is null
        var ex = Record.Exception(() =>
        {
            bool pressed = InvokeIsPressed(Key.A);
            Assert.False(pressed, "IsPressed must return false when Keyboard.current is null.");
        });

        Assert.Null(ex);
    }

    [Fact]
    public void EdgeCase1_KeyboardNull_CheckKeys_ReturnsSafelyWithoutException()
    {
        // Register key actions to ensure CheckKeys has work to inspect
        bool callbackFired = false;
        Input.OnKeyPressed(Key.Space, () => callbackFired = true);

        var ex = Record.Exception(() =>
        {
            ReflectionHelper.InvokeCheckKeys();
        });

        Assert.Null(ex);
        Assert.False(callbackFired, "Callbacks should not fire when Keyboard.current is null.");
    }

    // =========================================================================
    // Edge Case 2: Listener on Key.A throws during Input.TriggerKey
    // Does the next listener still get called?
    // =========================================================================

    [Fact]
    public void EdgeCase2_TriggerKey_ThrowingListener_NextListenerStillExecutes()
    {
        bool listener1Executed = false;
        bool listener2Executed = false;
        bool listener3Executed = false;
        bool listener4Executed = false;

        Action listener1 = () =>
        {
            listener1Executed = true;
            throw new InvalidOperationException("Adversarial crash from Listener 1");
        };

        Action listener2 = () =>
        {
            listener2Executed = true;
        };

        Action listener3 = () =>
        {
            listener3Executed = true;
            throw new ApplicationException("Adversarial crash from Listener 3");
        };

        Action listener4 = () =>
        {
            listener4Executed = true;
        };

        Input.OnKeyPressed(Key.A, listener1);
        Input.OnKeyPressed(Key.A, listener2);
        Input.OnKeyPressed(Key.A, listener3);
        Input.OnKeyPressed(Key.A, listener4);

        var ex = Record.Exception(() =>
        {
            ReflectionHelper.InvokeTriggerKey(Key.A);
        });

        Assert.Null(ex);
        Assert.True(listener1Executed, "Listener 1 should have been executed.");
        Assert.True(listener2Executed, "Listener 2 MUST be executed despite Listener 1 throwing.");
        Assert.True(listener3Executed, "Listener 3 should have been executed.");
        Assert.True(listener4Executed, "Listener 4 MUST be executed despite Listener 3 throwing.");
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Adversarial crash from Listener 1"));
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Adversarial crash from Listener 3"));
    }

    // =========================================================================
    // Edge Case 3: Listener registers or removes keys inside callback during invocation
    // Does it throw InvalidOperationException (collection modified) or survive?
    // =========================================================================

    [Fact]
    public void EdgeCase3_TriggerKey_MutateRegistrationsInsideCallback_SurvivesWithoutException()
    {
        bool callback2Fired = false;
        bool dynamicCallbackFired = false;

        Action dynamicCallback = () => dynamicCallbackFired = true;

        Action callback1 = null!;
        callback1 = () =>
        {
            // Adversarial mutation: remove self, add another key listener
            Input.RemoveKeyPressed(Key.B, callback1);
            Input.OnKeyPressed(Key.C, dynamicCallback);
            Input.OnKeyPressed(Key.B, () => { });
        };

        Action callback2 = () =>
        {
            callback2Fired = true;
            // Remove a different key altogether
            Input.RemoveKeyPressed(Key.C, dynamicCallback);
        };

        Input.OnKeyPressed(Key.B, callback1);
        Input.OnKeyPressed(Key.B, callback2);

        var ex = Record.Exception(() =>
        {
            ReflectionHelper.InvokeTriggerKey(Key.B);
        });

        Assert.Null(ex);
        Assert.True(callback2Fired, "Callback 2 should have fired.");
        Assert.False(dynamicCallbackFired, "Dynamic callback was unregistered before being triggered.");
    }

    [Fact]
    public void EdgeCase3_CheckKeys_SnapshotGuaranteesNoCollectionModifiedException()
    {
        // Populate multiple keys
        for (int i = 0; i < 20; i++)
        {
            var key = (Key)((int)Key.Digit0 + (i % 10));
            Input.OnKeyPressed(key, () =>
            {
                // Mutate dictionary during iteration
                Input.OnKeyPressed(Key.Escape, () => { });
                Input.RemoveKeyPressed(Key.Digit0, () => { });
            });
        }

        var ex = Record.Exception(() =>
        {
            ReflectionHelper.InvokeCheckKeys();
        });

        Assert.Null(ex);
    }

    // =========================================================================
    // Edge Case 4: OnKeyPressed called with null Action
    // =========================================================================

    [Fact]
    public void EdgeCase4_OnKeyPressed_NullAction_SafelyIgnoredWithoutException()
    {
        var ex = Record.Exception(() =>
        {
            Input.OnKeyPressed(Key.F1, null!);
            Input.RemoveKeyPressed(Key.F1, null!);
        });

        Assert.Null(ex);

        // Verify TriggerKey on Key.F1 does nothing and does not throw
        var triggerEx = Record.Exception(() =>
        {
            ReflectionHelper.InvokeTriggerKey(Key.F1);
        });

        Assert.Null(triggerEx);
    }

    // =========================================================================
    // Edge Case 5: ModLoader corrupted assembly and throwing mod classes
    // Does it continue loading subsequent mods?
    // =========================================================================

    [Fact]
    public void EdgeCase5_CorruptAssembly_LoadMod_DoesNotCrashAndLogsError()
    {
        string corruptPath = Path.Combine(_tempDir, "CorruptAssembly.dll");
        File.WriteAllBytes(corruptPath, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x11, 0x22, 0x33 });

        var loader = CreateModLoaderInstance();

        var ex = Record.Exception(() =>
        {
            ReflectionHelper.InvokeLoadMod(loader, corruptPath);
        });

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Corrupt or invalid mod assembly"));
    }

    [Fact]
    public void EdgeCase5_ThrowingModLoadMethod_DoesNotHaltSubsequentModsInSameAssembly()
    {
        string source = @"
using System;
using DeadshotModAPI;

public class FailingMod : IDeadshotMod
{
    public string Name => ""FailingMod"";
    public string Description => ""Throws in Load"";
    public string Creator => ""Adversary"";
    public string Version => ""0.0.1"";

    public void Load()
    {
        throw new InvalidOperationException(""Critical mod init crash"");
    }
}

public class SurvivingMod : IDeadshotMod
{
    public string Name => ""SurvivingMod"";
    public string Description => ""Should survive and load"";
    public string Creator => ""Author"";
    public string Version => ""1.0.0"";

    public bool HasLoaded { get; private set; }

    public void Load()
    {
        HasLoaded = true;
    }
}
";
        string dllPath = Path.Combine(_tempDir, "DualModAssembly.dll");
        TestAssemblyBuilder.CompileAssembly(source, dllPath, "DualModAssembly");

        var loader = CreateModLoaderInstance();

        var ex = Record.Exception(() =>
        {
            ReflectionHelper.InvokeLoadMod(loader, dllPath);
        });

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Critical mod init crash"));

        // Verify SurvivingMod is in LoadedMods
        var loaded = ModLoader.LoadedMods.Find(m => m.Name == "SurvivingMod");
        Assert.NotNull(loaded);
        Assert.Equal("SurvivingMod", loaded.Name);

        // Verify FailingMod is NOT in LoadedMods
        var failed = ModLoader.LoadedMods.Find(m => m.Name == "FailingMod");
        Assert.Null(failed);
    }

    [Fact]
    public void EdgeCase5_ThrowingConstructor_DoesNotHaltSubsequentMods()
    {
        string source = @"
using System;
using DeadshotModAPI;

public class ThrowingCtorMod : IDeadshotMod
{
    public ThrowingCtorMod()
    {
        throw new TypeInitializationException(""ThrowingCtorMod"", new Exception(""Constructor failure""));
    }

    public string Name => ""ThrowingCtorMod"";
    public string Description => ""Desc"";
    public string Creator => ""Author"";
    public string Version => ""1.0"";
    public void Load() {}
}

public class SecondGoodMod : IDeadshotMod
{
    public string Name => ""SecondGoodMod"";
    public string Description => ""Good Desc"";
    public string Creator => ""Good Author"";
    public string Version => ""1.0"";
    public void Load() {}
}
";
        string dllPath = Path.Combine(_tempDir, "ThrowingCtorAssembly.dll");
        TestAssemblyBuilder.CompileAssembly(source, dllPath, "ThrowingCtorAssembly");

        var loader = CreateModLoaderInstance();

        var ex = Record.Exception(() =>
        {
            ReflectionHelper.InvokeLoadMod(loader, dllPath);
        });

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Failed to instantiate or load mod"));

        var good = ModLoader.LoadedMods.Find(m => m.Name == "SecondGoodMod");
        Assert.NotNull(good);
    }

    [Fact]
    public void EdgeCase5_ThrowingPropertyGetters_DefaultsSafelyAndLoadsMod()
    {
        string source = @"
using System;
using DeadshotModAPI;

public class ThrowingPropertiesMod : IDeadshotMod
{
    public string Name => throw new ApplicationException(""Name getter explosion"");
    public string Description => throw new ApplicationException(""Desc explosion"");
    public string Creator => throw new ApplicationException(""Creator explosion"");
    public string Version => throw new ApplicationException(""Version explosion"");

    public bool LoadedSuccessfully { get; private set; }

    public void Load()
    {
        LoadedSuccessfully = true;
    }
}
";
        string dllPath = Path.Combine(_tempDir, "ThrowingPropertiesAssembly.dll");
        TestAssemblyBuilder.CompileAssembly(source, dllPath, "ThrowingPropertiesAssembly");

        var loader = CreateModLoaderInstance();

        var ex = Record.Exception(() =>
        {
            ReflectionHelper.InvokeLoadMod(loader, dllPath);
        });

        Assert.Null(ex);
        Assert.Single(ModLoader.LoadedMods);
        Assert.Contains(TestLoggerMock.LoggedWarnings, w => w.Contains("Failed to read mod name"));
    }

    [Fact]
    public void EdgeCase_LoadMod_NullOrWhitespaceOrNonExistentPath_HandledDefensively()
    {
        var loader = CreateModLoaderInstance();

        var exNull = Record.Exception(() => ReflectionHelper.InvokeLoadMod(loader, null!));
        var exEmpty = Record.Exception(() => ReflectionHelper.InvokeLoadMod(loader, "   "));
        var exNonExistent = Record.Exception(() => ReflectionHelper.InvokeLoadMod(loader, Path.Combine(_tempDir, "DoesNotExist.dll")));

        Assert.Null(exNull);
        Assert.Null(exEmpty);
        Assert.Null(exNonExistent);
        Assert.Contains(TestLoggerMock.LoggedWarnings, w => w.Contains("Mod file path is null or empty"));
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Mod file does not exist"));
    }

    [Fact]
    public void StressTest_HighVolumeListeners_AlternatingExceptions_AllValidListenersExecute()
    {
        int totalListeners = 500;
        int successfulInvocations = 0;

        for (int i = 0; i < totalListeners; i++)
        {
            int index = i;
            if (index % 2 == 0)
            {
                Input.OnKeyPressed(Key.F8, () =>
                {
                    successfulInvocations++;
                });
            }
            else
            {
                Input.OnKeyPressed(Key.F8, () =>
                {
                    throw new Exception($"Stress exception at index {index}");
                });
            }
        }

        var ex = Record.Exception(() =>
        {
            ReflectionHelper.InvokeTriggerKey(Key.F8);
        });

        Assert.Null(ex);
        Assert.Equal(250, successfulInvocations);
        Assert.Equal(250, TestLoggerMock.LoggedErrors.FindAll(e => e.Contains("Stress exception")).Count);
    }

    [Fact]
    public void StressTest_ReentrantTriggerKey_ExecutesSafely()
    {
        bool f10Executed = false;
        bool f9SecondExecuted = false;

        Input.OnKeyPressed(Key.F9, () =>
        {
            // Reentrant trigger of another key
            ReflectionHelper.InvokeTriggerKey(Key.F10);
        });

        Input.OnKeyPressed(Key.F9, () =>
        {
            f9SecondExecuted = true;
        });

        Input.OnKeyPressed(Key.F10, () =>
        {
            f10Executed = true;
            // Also mutate during reentrant call
            Input.OnKeyPressed(Key.F11, () => { });
        });

        var ex = Record.Exception(() =>
        {
            ReflectionHelper.InvokeTriggerKey(Key.F9);
        });

        Assert.Null(ex);
        Assert.True(f10Executed, "Reentrant F10 should have executed.");
        Assert.True(f9SecondExecuted, "Subsequent F9 listener should have executed.");
    }

    [Fact]
    public void EdgeCase_SceneManager_Load_InvalidInputs_DoNotThrow()
    {
        var exNull = Record.Exception(() => SceneManager.Load((string)null!));
        var exEmpty = Record.Exception(() => SceneManager.Load("   "));
        var exNegative = Record.Exception(() => SceneManager.Load(-1));
        var exNegLarge = Record.Exception(() => SceneManager.Load(-999));

        Assert.Null(exNull);
        Assert.Null(exEmpty);
        Assert.Null(exNegative);
        Assert.Null(exNegLarge);

        Assert.Contains(TestLoggerMock.LoggedWarnings, w => w.Contains("sceneName is null, empty, or whitespace"));
        Assert.Contains(TestLoggerMock.LoggedWarnings, w => w.Contains("invalid sceneIndex '-1'"));
    }

    [Fact]
    public void EdgeCase_GameManager_RestartLevel_DefensivelyReturnsWithoutUnhandledException()
    {
        // Calling RestartLevel in uninitialized environment
        var ex = Record.Exception(() =>
        {
            GameManager.RestartLevel(true);
            GameManager.RestartLevel(false);
        });

        Assert.Null(ex);
    }
}

