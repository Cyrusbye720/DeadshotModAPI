using System;
using System.Collections.Generic;
using System.Reflection;

namespace DeadshotModAPI.Tests.TestHelpers;

/// <summary>
/// Helper utilities to invoke internal and private members for defensive and isolated testing.
/// </summary>
public static class ReflectionHelper
{
    static ReflectionHelper()
    {
        TestLoggerMock.Initialize();
    }

    private static readonly MethodInfo TriggerKeyMethod =
        typeof(Input).GetMethod("TriggerKey", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("Could not find Input.TriggerKey method.");

    private static readonly MethodInfo CheckKeysMethod =
        typeof(Input).GetMethod("CheckKeys", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("Could not find Input.CheckKeys method.");

    private static readonly MethodInfo LoadModMethod =
        typeof(ModLoader).GetMethod("LoadMod", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("Could not find ModLoader.LoadMod method.");

    private static readonly MethodInfo LoadModsMethod =
        typeof(ModLoader).GetMethod("LoadMods", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("Could not find ModLoader.LoadMods method.");

    private static readonly MethodInfo LoadModsBundleMethod =
        typeof(SceneManager).GetMethod("LoadModsBundle", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("Could not find SceneManager.LoadModsBundle method.");

    private static readonly FieldInfo KeyActionsField =
        typeof(Input).GetField("_keyActions", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("Could not find Input._keyActions field.");

    /// <summary>
    /// Invokes internal Input.TriggerKey(Key).
    /// </summary>
    public static void InvokeTriggerKey(Key key)
    {
        TriggerKeyMethod.Invoke(null, new object[] { key });
    }

    /// <summary>
    /// Invokes internal Input.CheckKeys().
    /// </summary>
    public static void InvokeCheckKeys()
    {
        CheckKeysMethod.Invoke(null, null);
    }

    /// <summary>
    /// Invokes private ModLoader.LoadMod(path).
    /// </summary>
    public static void InvokeLoadMod(ModLoader loader, string path)
    {
        LoadModMethod.Invoke(loader, new object[] { path });
    }

    /// <summary>
    /// Invokes private ModLoader.LoadMods().
    /// </summary>
    public static void InvokeLoadMods(ModLoader loader)
    {
        LoadModsMethod.Invoke(loader, null);
    }

    /// <summary>
    /// Invokes internal SceneManager.LoadModsBundle().
    /// </summary>
    public static void InvokeLoadModsBundle()
    {
        LoadModsBundleMethod.Invoke(null, null);
    }

    /// <summary>
    /// Clears registered key actions between tests to ensure test isolation.
    /// </summary>
    public static void ResetInputState()
    {
        if (KeyActionsField.GetValue(null) is Dictionary<Key, Action> dict)
        {
            dict.Clear();
        }
    }

    /// <summary>
    /// Clears ModLoader.LoadedMods between tests to ensure test isolation.
    /// </summary>
    public static void ResetModLoaderState()
    {
        ModLoader.LoadedMods.Clear();
    }
}
