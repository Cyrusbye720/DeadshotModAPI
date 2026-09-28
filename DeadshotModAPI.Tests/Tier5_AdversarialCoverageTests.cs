using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using DeadshotModAPI.Tests.TestHelpers;
using HarmonyLib;
using Mono.Cecil;
using Xunit;

namespace DeadshotModAPI.Tests;

/// <summary>
/// Tier 5 Adversarial Coverage Hardening Suite.
/// White-box stress tests verifying all corner cases, exception catches, directory discovery,
/// multicast delegate mutations, and plugin contracts across DeadshotModAPI production code.
/// </summary>
public class Tier5_AdversarialCoverageTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ModLoader _loader;

    public Tier5_AdversarialCoverageTests()
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
        TestLoggerMock.OverrideBepInExRootPath = null;
        TestLoggerMock.ResetLogs();
        ReflectionHelper.ResetInputState();
        ReflectionHelper.ResetModLoaderState();

        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    #region 1. Plugin Entrypoint Contract Tests (Class1.cs)

    [Fact]
    public void Plugin_BepInPluginAttribute_HasAccurateMetadata()
    {
        var asmDef = AssemblyDefinition.ReadAssembly(typeof(Plugin).Assembly.Location);
        var pluginType = asmDef.MainModule.GetType("DeadshotModAPI.Plugin");

        Assert.NotNull(pluginType);
        var attr = pluginType.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "BepInPlugin");

        Assert.NotNull(attr);
        Assert.True(attr.ConstructorArguments.Count >= 3);
        Assert.Equal("com.subaka.deadshotmodapi", attr.ConstructorArguments[0].Value);
        Assert.Equal("Deadshot Mod Api", attr.ConstructorArguments[1].Value);
        Assert.Equal("0.0.1", attr.ConstructorArguments[2].Value);
    }

    [Fact]
    public void Plugin_InheritsFromBasePlugin_AndDeclaresPublicLoad()
    {
        var type = typeof(Plugin);
        Assert.True(typeof(BasePlugin).IsAssignableFrom(type), "Plugin must inherit from BasePlugin.");

        var loadMethod = type.GetMethod("Load", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(loadMethod);
        Assert.Equal(typeof(void), loadMethod.ReturnType);
        Assert.Empty(loadMethod.GetParameters());
    }

    [Fact]
    public void Plugin_Load_EmitsStartupInfoLog()
    {
        var plugin = (Plugin)FormatterServices.GetUninitializedObject(typeof(Plugin));

        // When Load() is invoked in headless mode, AddComponent<ModLoader>() may fail due to uninitialized IL2CPP GameObject,
        // but the startup log must have been emitted first.
        try
        {
            plugin.Load();
        }
        catch
        {
            // Expected in headless environment if Unity AddComponent fails
        }

        Assert.Contains(TestLoggerMock.LoggedInfo, msg => msg.Contains("Deadshot Mod API loaded."));
    }

    #endregion

    #region 2. Logger Standard Log Alias & Edge Cases (Logging/logger.cs)

    [Fact]
    public void Logger_Log_StandardAlias_RoutesToInfoAndCapturesMessage()
    {
        Logger.Log("Tier 5 adversarial logging message verification.");

        Assert.Contains(TestLoggerMock.LoggedInfo, msg => msg.Contains("Tier 5 adversarial logging message verification."));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Special characters: {0} {foo} %d %s \0 \t \r\n")]
    public void Logger_Log_HandlesNullEmptyAndSpecialStringsSafely(string message)
    {
        var ex = Record.Exception(() => Logger.Log(message!));
        Assert.Null(ex);
    }

    #endregion

    #region 3. ModLoader Directory Discovery & Resilient Lifecycle (Runtime/ModLoader.cs)

    [Fact]
    public void ModLoader_LoadMods_WhenBepInExRootPathThrows_LogsErrorAndReturnsSafely()
    {
        var harmony = new Harmony("DeadshotModAPI.Tests.ThrowBepPath");
        var getter = typeof(BepInEx.Paths).GetProperty("BepInExRootPath", BindingFlags.Public | BindingFlags.Static)?.GetGetMethod();
        Assert.NotNull(getter);

        var prefix = typeof(Tier5_AdversarialCoverageTests).GetMethod(nameof(PrefixThrowBepPath), BindingFlags.NonPublic | BindingFlags.Static);
        harmony.Patch(getter, new HarmonyMethod(prefix) { priority = Priority.First });
        try
        {
            var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMods(_loader));

            Assert.Null(ex);
            Assert.Contains(TestLoggerMock.LoggedErrors, err =>
                err.Contains("Failed to determine BepInEx root path", StringComparison.OrdinalIgnoreCase) &&
                err.Contains("Simulated failure accessing BepInExRootPath", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            harmony.Unpatch(getter, HarmonyPatchType.Prefix, "DeadshotModAPI.Tests.ThrowBepPath");
        }
    }

    private static bool PrefixThrowBepPath()
    {
        throw new InvalidOperationException("Simulated failure accessing BepInExRootPath");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void ModLoader_LoadMods_WhenBepInExRootPathEmptyOrWhitespace_LogsErrorAndReturnsSafely(string rootPath)
    {
        TestLoggerMock.OverrideBepInExRootPath = rootPath;

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMods(_loader));

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err =>
            err.Contains("BepInEx root path is null or empty. Cannot discover mods.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ModLoader_LoadMods_DirectoryDoesNotExist_CreatesModsFolderAndLogsInfo()
    {
        string fakeRoot = Path.Combine(_tempDir, "FakeBepInEx");
        Directory.CreateDirectory(fakeRoot);
        string expectedModsDir = Path.Combine(fakeRoot, "mods");

        Assert.False(Directory.Exists(expectedModsDir));
        TestLoggerMock.OverrideBepInExRootPath = fakeRoot;

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMods(_loader));

        Assert.Null(ex);
        Assert.True(Directory.Exists(expectedModsDir), "ModLoader.LoadMods must create the 'mods' directory if it does not exist.");
        Assert.Contains(TestLoggerMock.LoggedInfo, msg =>
            msg.Contains("Created mod directory", StringComparison.OrdinalIgnoreCase) &&
            msg.Contains(expectedModsDir, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ModLoader_LoadMods_RecursiveNestedDiscovery_DiscoversAndLoadsAllMods()
    {
        string fakeRoot = Path.Combine(_tempDir, "RecursiveBepInEx");
        string modsDir = Path.Combine(fakeRoot, "mods");
        string nestedFolderA = Path.Combine(modsDir, "FolderA", "SubA");
        string nestedFolderB = Path.Combine(modsDir, "FolderB");

        Directory.CreateDirectory(nestedFolderA);
        Directory.CreateDirectory(nestedFolderB);

        string modSourceA = @"
using DeadshotModAPI;
public class RecursiveModA : IDeadshotMod
{
    public string Name => ""RecursiveModA"";
    public string Description => ""Desc A"";
    public string Creator => ""Dev A"";
    public string Version => ""1.0.0"";
    public static bool Loaded = false;
    public void Load() { Loaded = true; }
}";
        string modSourceB = @"
using DeadshotModAPI;
public class RecursiveModB : IDeadshotMod
{
    public string Name => ""RecursiveModB"";
    public string Description => ""Desc B"";
    public string Creator => ""Dev B"";
    public string Version => ""2.0.0"";
    public static bool Loaded = false;
    public void Load() { Loaded = true; }
}";

        string dllPathA = Path.Combine(nestedFolderA, "ModA.dll");
        string dllPathB = Path.Combine(nestedFolderB, "ModB.dll");

        TestAssemblyBuilder.CompileAssembly(modSourceA, dllPathA, "RecursiveModAAssembly");
        TestAssemblyBuilder.CompileAssembly(modSourceB, dllPathB, "RecursiveModBAssembly");

        TestLoggerMock.OverrideBepInExRootPath = fakeRoot;

        // Act
        ReflectionHelper.InvokeLoadMods(_loader);

        // Assert
        Assert.Equal(2, ModLoader.LoadedMods.Count);
        var modA = ModLoader.LoadedMods.Find(m => m.Name == "RecursiveModA");
        var modB = ModLoader.LoadedMods.Find(m => m.Name == "RecursiveModB");

        Assert.NotNull(modA);
        Assert.NotNull(modB);

        var flagA = modA.GetType().GetField("Loaded", BindingFlags.Public | BindingFlags.Static);
        var flagB = modB.GetType().GetField("Loaded", BindingFlags.Public | BindingFlags.Static);

        Assert.True((bool)flagA!.GetValue(null)!);
        Assert.True((bool)flagB!.GetValue(null)!);
    }

    [Fact]
    public void ModLoader_Start_CatchesAndLogsUnexpectedExceptionSafely()
    {
        // When Start is called, even if internal LoadMods fails with an exception, Start does not leak it
        TestLoggerMock.OverrideBepInExRootPath = Path.Combine(_tempDir, "StartRoot");
        Directory.CreateDirectory(TestLoggerMock.OverrideBepInExRootPath);

        var ex = Record.Exception(() => _loader.Start());
        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedInfo, msg => msg.Contains("Deadshot Mod API ModLoader Start."));
    }

    [Fact]
    public void ModLoader_Update_CatchesAndLogsUnexpectedExceptionSafely()
    {
        // Update calls Input.CheckKeys() inside defensive try-catch
        var ex = Record.Exception(() => _loader.Update());
        Assert.Null(ex);
    }

    #endregion

    #region 4. ModLoader Type Filtering & Reflection Resilience (Runtime/ModLoader.cs)

    [Fact]
    public void ModLoader_LoadMod_InterfaceExtendingIDeadshotMod_IsIgnored()
    {
        string source = @"
using DeadshotModAPI;
public interface IExtendedModInterface : IDeadshotMod
{
    void ExtendedMethod();
}";
        string dllPath = Path.Combine(_tempDir, "InterfaceMod.dll");
        TestAssemblyBuilder.CompileAssembly(source, dllPath, "InterfaceModAssembly");

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, dllPath));

        Assert.Null(ex);
        Assert.Empty(ModLoader.LoadedMods);
    }

    [Fact]
    public void ModLoader_LoadMod_ClassNotImplementingIDeadshotMod_IsIgnored()
    {
        string source = @"
public class PlainStandaloneClass
{
    public void SomeMethod() {}
}";
        string dllPath = Path.Combine(_tempDir, "PlainClass.dll");
        TestAssemblyBuilder.CompileAssembly(source, dllPath, "PlainClassAssembly");

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, dllPath));

        Assert.Null(ex);
        Assert.Empty(ModLoader.LoadedMods);
    }

    [Fact]
    public void ModLoader_LoadMod_MissingParameterlessConstructor_LogsErrorAndDoesNotCrash()
    {
        string source = @"
using DeadshotModAPI;
public class NoParameterlessCtorMod : IDeadshotMod
{
    public NoParameterlessCtorMod(string requiredArgument)
    {
    }

    public string Name => ""NoDefaultCtorMod"";
    public string Description => ""Desc"";
    public string Creator => ""Author"";
    public string Version => ""1.0.0"";
    public void Load() {}
}";
        string dllPath = Path.Combine(_tempDir, "NoDefaultCtorMod.dll");
        TestAssemblyBuilder.CompileAssembly(source, dllPath, "NoDefaultCtorModAssembly");

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, dllPath));

        Assert.Null(ex);
        Assert.Empty(ModLoader.LoadedMods);
        Assert.Contains(TestLoggerMock.LoggedErrors, err =>
            err.Contains("Failed to instantiate or load mod", StringComparison.OrdinalIgnoreCase) &&
            err.Contains("NoParameterlessCtorMod", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ModLoader_LoadMod_ReflectionTypeLoadException_ProcessesRemainingTypesAndLogsWarnings()
    {
        string source = @"
using DeadshotModAPI;
public class ResilientModAlpha : IDeadshotMod
{
    public string Name => ""ResilientModAlpha"";
    public string Description => ""Desc Alpha"";
    public string Creator => ""Dev Alpha"";
    public string Version => ""1.0"";
    public static bool Loaded = false;
    public void Load() { Loaded = true; }
}

public class ResilientModBeta : IDeadshotMod
{
    public string Name => ""ResilientModBeta"";
    public string Description => ""Desc Beta"";
    public string Creator => ""Dev Beta"";
    public string Version => ""1.0"";
    public static bool Loaded = false;
    public void Load() { Loaded = true; }
}";
        string dllPath = Path.Combine(_tempDir, "RtleMod.dll");
        TestAssemblyBuilder.CompileAssembly(source, dllPath, "RtleModAssembly");

        // Load the assembly to inspect its types
        var asm = Assembly.LoadFrom(dllPath);
        var types = asm.GetTypes();
        var alphaType = Array.Find(types, t => t.Name == "ResilientModAlpha")!;
        var betaType = Array.Find(types, t => t.Name == "ResilientModBeta")!;

        // Simulate ReflectionTypeLoadException where ex.Types contains [alphaType, null, betaType]
        // and ex.LoaderExceptions contains an exception
        var harmony = new Harmony("DeadshotModAPI.Tests.RtleTest");
        var getTypesMethod = typeof(Assembly).GetMethod("GetTypes", Type.EmptyTypes);

        var prefixMethod = typeof(Tier5_AdversarialCoverageTests).GetMethod(nameof(PrefixThrowRtle), BindingFlags.NonPublic | BindingFlags.Static);
        _targetRtleAssembly = asm;
        _simulatedAlphaType = alphaType;
        _simulatedBetaType = betaType;

        harmony.Patch(getTypesMethod, new HarmonyMethod(prefixMethod));
        try
        {
            var ex = Record.Exception(() => ReflectionHelper.InvokeLoadMod(_loader, dllPath));

            Assert.Null(ex);

            // Verify ReflectionTypeLoadException was caught and logged as warning
            Assert.Contains(TestLoggerMock.LoggedWarnings, w =>
                w.Contains("Type load warnings encountered while inspecting", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(TestLoggerMock.LoggedWarnings, w =>
                w.Contains("Loader exception: Simulated type load exception for adversarial test", StringComparison.OrdinalIgnoreCase));

            // Verify both valid types survived despite the exception and null entries in ex.Types!
            Assert.Equal(2, ModLoader.LoadedMods.Count);
            var modAlpha = ModLoader.LoadedMods.Find(m => m.Name == "ResilientModAlpha");
            var modBeta = ModLoader.LoadedMods.Find(m => m.Name == "ResilientModBeta");

            Assert.NotNull(modAlpha);
            Assert.NotNull(modBeta);

            var flagAlpha = modAlpha.GetType().GetField("Loaded", BindingFlags.Public | BindingFlags.Static);
            var flagBeta = modBeta.GetType().GetField("Loaded", BindingFlags.Public | BindingFlags.Static);

            Assert.True((bool)flagAlpha!.GetValue(null)!);
            Assert.True((bool)flagBeta!.GetValue(null)!);
        }
        finally
        {
            harmony.Unpatch(getTypesMethod, HarmonyPatchType.Prefix, "DeadshotModAPI.Tests.RtleTest");
            _targetRtleAssembly = null;
            _simulatedAlphaType = null;
            _simulatedBetaType = null;
        }
    }

    private static Assembly _targetRtleAssembly;
    private static Type _simulatedAlphaType;
    private static Type _simulatedBetaType;

    private static bool PrefixThrowRtle(Assembly __instance, ref Type[] __result)
    {
        if (ReferenceEquals(__instance, _targetRtleAssembly))
        {
            var types = new Type[] { _simulatedAlphaType, null!, _simulatedBetaType };
            var exceptions = new Exception[] { new TypeLoadException("Simulated type load exception for adversarial test.") };
            throw new ReflectionTypeLoadException(types, exceptions);
        }
        return true;
    }

    #endregion

    #region 5. Input Multicast Removal & String Formatting Edge Cases (Input/InputManager.cs)

    [Fact]
    public void Input_RemoveKeyPressed_UnregisteredDelegateOnKeyWithExistingListeners_PreservesExistingListeners()
    {
        bool listener1Fired = false;
        Action action1 = () => listener1Fired = true;
        Action unregisteredAction = () => { };

        Input.OnKeyPressed(Key.F6, action1);

        // Remove an action that was NEVER registered on Key.F6
        Input.RemoveKeyPressed(Key.F6, unregisteredAction);

        // Trigger Key.F6
        ReflectionHelper.InvokeTriggerKey(Key.F6);

        Assert.True(listener1Fired, "Existing registered listener must remain functional after removing an unregistered delegate.");
    }

    [Fact]
    public void Input_RemoveKeyPressed_MultipleDelegates_RemovesOnlyTargetDelegate()
    {
        bool listener1Fired = false;
        bool listener2Fired = false;
        Action action1 = () => listener1Fired = true;
        Action action2 = () => listener2Fired = true;

        Input.OnKeyPressed(Key.F7, action1);
        Input.OnKeyPressed(Key.F7, action2);

        // Remove only action1
        Input.RemoveKeyPressed(Key.F7, action1);

        // Trigger Key.F7
        ReflectionHelper.InvokeTriggerKey(Key.F7);

        Assert.False(listener1Fired, "action1 should have been detached.");
        Assert.True(listener2Fired, "action2 must still execute.");
    }

    [Fact]
    public void Input_RemoveKeyPressed_DoubleRemovalOfSameDelegate_IsIdempotent()
    {
        int invocationCount = 0;
        Action action = () => invocationCount++;

        Input.OnKeyPressed(Key.F8, action);

        // Remove twice
        Input.RemoveKeyPressed(Key.F8, action);
        var ex = Record.Exception(() => Input.RemoveKeyPressed(Key.F8, action));

        Assert.Null(ex);

        // Trigger Key.F8
        ReflectionHelper.InvokeTriggerKey(Key.F8);

        Assert.Equal(0, invocationCount);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("\t\r\n", "\t\r\n")]
    [InlineData("1Digit", "1Digit")]
    [InlineData("!Bang", "!Bang")]
    [InlineData("X", "x")]
    [InlineData("AlphaBetaGamma", "alphaBetaGamma")]
    public void Input_ToCamelCase_EdgeCases_TransformsSafelyWithoutIndexOutOfRange(string input, string expected)
    {
        string result = input.ToCamelCase();
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Input_TriggerKey_MultipleThrowingListeners_IsolatesEachAndLogsAllErrors()
    {
        bool listener2Fired = false;

        Action action1 = () => throw new InvalidOperationException("Adversarial error from Action 1");
        Action action2 = () => listener2Fired = true;
        Action action3 = () => throw new ArgumentException("Adversarial error from Action 3");

        Input.OnKeyPressed(Key.F12, action1);
        Input.OnKeyPressed(Key.F12, action2);
        Input.OnKeyPressed(Key.F12, action3);

        ReflectionHelper.InvokeTriggerKey(Key.F12);

        Assert.True(listener2Fired, "Action 2 must execute even when Action 1 throws.");
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Adversarial error from Action 1"));
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Adversarial error from Action 3"));
    }

    #endregion

    #region 6. SceneManager & GameManager Boundary Isolation (SceneManager & GameManager)

    [Theory]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void SceneManager_LoadModsBundle_WhitespaceBepInExRoot_LogsErrorAndReturnsSafely(string whitespacePath)
    {
        TestLoggerMock.OverrideBepInExRootPath = whitespacePath;

        var ex = Record.Exception(() => ReflectionHelper.InvokeLoadModsBundle());

        Assert.Null(ex);
        Assert.Contains(TestLoggerMock.LoggedErrors, err =>
            err.Contains("BepInEx root path is null or empty", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t\r\n")]
    [InlineData("SceneWithSpaces 123")]
    public void SceneManager_Load_StringVariations_ExecutesDefensively(string sceneName)
    {
        var ex = Record.Exception(() => SceneManager.Load(sceneName));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(int.MaxValue)]
    public void SceneManager_Load_IntExtremeBoundaries_ExecutesDefensively(int sceneIndex)
    {
        var ex = Record.Exception(() => SceneManager.Load(sceneIndex));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GameManager_RestartLevel_CutsceneFlagVariations_DefensivelyHandlesUninitializedRuntime(bool playCutscene)
    {
        for (int i = 0; i < 3; i++)
        {
            var ex = Record.Exception(() => GameManager.RestartLevel(playCutscene));
            Assert.Null(ex);
        }
    }

    #endregion
}
