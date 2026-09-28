using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace DeadshotModAPI.Tests.TestHelpers;

public static class TestLoggerMock
{
    private static Harmony _harmony;
    private static readonly object _sync = new();

    private static readonly List<string> _loggedInfo = new();
    private static readonly List<string> _loggedWarnings = new();
    private static readonly List<string> _loggedErrors = new();
    private static readonly List<string> _loggedDebug = new();

    public static List<string> LoggedInfo
    {
        get { lock (_sync) return _loggedInfo.ToList(); }
    }

    public static List<string> LoggedWarnings
    {
        get { lock (_sync) return _loggedWarnings.ToList(); }
    }

    public static List<string> LoggedErrors
    {
        get { lock (_sync) return _loggedErrors.ToList(); }
    }

    public static List<string> LoggedDebug
    {
        get { lock (_sync) return _loggedDebug.ToList(); }
    }

    public static string OverrideBepInExRootPath = null;

    public static void Initialize()
    {
        if (_harmony != null) return;

        _harmony = new Harmony("DeadshotModAPI.Tests.Mock");

        // 1. Patch Logger.Info, Warning, Error, Debug
        var prefixInfo = typeof(TestLoggerMock).GetMethod(nameof(PrefixInfo), BindingFlags.NonPublic | BindingFlags.Static);
        var prefixWarn = typeof(TestLoggerMock).GetMethod(nameof(PrefixWarning), BindingFlags.NonPublic | BindingFlags.Static);
        var prefixErr = typeof(TestLoggerMock).GetMethod(nameof(PrefixError), BindingFlags.NonPublic | BindingFlags.Static);
        var prefixDbg = typeof(TestLoggerMock).GetMethod(nameof(PrefixDebug), BindingFlags.NonPublic | BindingFlags.Static);

        _harmony.Patch(typeof(Logger).GetMethod("Info", new[] { typeof(string) }), new HarmonyMethod(prefixInfo));
        _harmony.Patch(typeof(Logger).GetMethod("Warning", new[] { typeof(string) }), new HarmonyMethod(prefixWarn));
        _harmony.Patch(typeof(Logger).GetMethod("Error", new[] { typeof(string) }), new HarmonyMethod(prefixErr));
        _harmony.Patch(typeof(Logger).GetMethod("Debug", new[] { typeof(string) }), new HarmonyMethod(prefixDbg));

        // 2. Patch BepInEx.Paths.BepInExRootPath getter
        var rootGetter = typeof(BepInEx.Paths).GetProperty("BepInExRootPath", BindingFlags.Public | BindingFlags.Static)?.GetGetMethod();
        if (rootGetter != null)
        {
            var prefixRoot = typeof(TestLoggerMock).GetMethod(nameof(PrefixGetBepInExRootPath), BindingFlags.NonPublic | BindingFlags.Static);
            try { _harmony.Patch(rootGetter, new HarmonyMethod(prefixRoot)); } catch { }
        }

        // 3. Patch UnityEngine.Object.op_Inequality
        var opInequality = typeof(UnityEngine.Object).GetMethod("op_Inequality", BindingFlags.Public | BindingFlags.Static);
        if (opInequality != null)
        {
            var prefixOpInEq = typeof(TestLoggerMock).GetMethod(nameof(PrefixOpInequality), BindingFlags.NonPublic | BindingFlags.Static);
            try { _harmony.Patch(opInequality, new HarmonyMethod(prefixOpInEq)); } catch { }
        }

        // 4. Patch Il2CppSystem.String.op_Implicit(string)
        var prefixOpImplicitString = typeof(TestLoggerMock).GetMethod(nameof(PrefixOpImplicitString), BindingFlags.NonPublic | BindingFlags.Static);
        foreach (var m in typeof(Il2CppSystem.String).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (m.Name == "op_Implicit" && m.ReturnType == typeof(Il2CppSystem.String))
            {
                var pars = m.GetParameters();
                if (pars.Length == 1 && pars[0].ParameterType == typeof(string))
                {
                    try { _harmony.Patch(m, new HarmonyMethod(prefixOpImplicitString)); } catch { }
                }
            }
        }

        // 5. Patch Il2CppSystem.Object.op_Implicit(string)
        var prefixOpImplicitObject = typeof(TestLoggerMock).GetMethod(nameof(PrefixOpImplicitObject), BindingFlags.NonPublic | BindingFlags.Static);
        foreach (var m in typeof(Il2CppSystem.Object).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (m.Name == "op_Implicit" && m.ReturnType == typeof(Il2CppSystem.Object))
            {
                var pars = m.GetParameters();
                if (pars.Length == 1 && pars[0].ParameterType == typeof(string))
                {
                    try { _harmony.Patch(m, new HarmonyMethod(prefixOpImplicitObject)); } catch { }
                }
            }
        }

        // 6. Patch UnityEngine.Debug.Log, LogWarning, LogError
        var debugType = typeof(UnityEngine.Debug);
        var prefixDebugSkip = typeof(TestLoggerMock).GetMethod(nameof(PrefixDebugSkip), BindingFlags.NonPublic | BindingFlags.Static);

        foreach (var m in debugType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (m.Name is "Log" or "LogWarning" or "LogError")
            {
                try { _harmony.Patch(m, new HarmonyMethod(prefixDebugSkip)); } catch { }
            }
        }
    }

    public static void ResetLogs()
    {
        lock (_sync)
        {
            _loggedInfo.Clear();
            _loggedWarnings.Clear();
            _loggedErrors.Clear();
            _loggedDebug.Clear();
        }
    }

    private static bool PrefixInfo(string message)
    {
        lock (_sync) _loggedInfo.Add(message);
        return false;
    }

    private static bool PrefixWarning(string message)
    {
        lock (_sync) _loggedWarnings.Add(message);
        return false;
    }

    private static bool PrefixError(string message)
    {
        lock (_sync) _loggedErrors.Add(message);
        return false;
    }

    private static bool PrefixDebug(string message)
    {
        lock (_sync) _loggedDebug.Add(message);
        return false;
    }

    private static bool PrefixGetBepInExRootPath(ref string __result)
    {
        if (OverrideBepInExRootPath != null)
        {
            __result = OverrideBepInExRootPath;
            return false;
        }
        return true;
    }

    private static bool PrefixOpInequality(UnityEngine.Object x, UnityEngine.Object y, ref bool __result)
    {
        bool xNull = ReferenceEquals(x, null);
        bool yNull = ReferenceEquals(y, null);
        __result = (xNull != yNull) || (!xNull && !yNull && !ReferenceEquals(x, y));
        return false;
    }

    private static bool PrefixOpImplicitString(string s, ref Il2CppSystem.String __result)
    {
        __result = null;
        return false;
    }

    private static bool PrefixOpImplicitObject(string s, ref Il2CppSystem.Object __result)
    {
        __result = null;
        return false;
    }

    private static bool PrefixDebugSkip()
    {
        return false;
    }
}
