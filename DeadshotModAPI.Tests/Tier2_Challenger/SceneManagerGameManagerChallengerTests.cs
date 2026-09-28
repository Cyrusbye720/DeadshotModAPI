using System;
using System.Reflection;
using DeadshotModAPI.Tests.TestHelpers;
using HarmonyLib;
using Xunit;

namespace DeadshotModAPI.Tests.Tier2_Challenger;

/// <summary>
/// Adversarial edge case tests for SceneManager and GameManager error handling and null-safety.
/// Executed by Challenger 2 for Milestone 2.
/// </summary>
public class SceneManagerGameManagerChallengerTests
{
    private static bool _patchesInitialized;

    public SceneManagerGameManagerChallengerTests()
    {
        TestLoggerMock.Initialize();
        TestLoggerMock.ResetLogs();
        InitializePatches();
    }

    private static void InitializePatches()
    {
        if (_patchesInitialized) return;
        _patchesInitialized = true;

        var harmony = new Harmony("DeadshotModAPI.Tests.Challenger2");

        // Patch UnityEngine.Object.op_Inequality
        var opInequality = typeof(UnityEngine.Object).GetMethod("op_Inequality", BindingFlags.Public | BindingFlags.Static);
        if (opInequality != null)
        {
            var prefix = typeof(SceneManagerGameManagerChallengerTests).GetMethod(nameof(PrefixOpInequality), BindingFlags.NonPublic | BindingFlags.Static);
            try
            {
                harmony.Patch(opInequality, new HarmonyMethod(prefix));
            }
            catch { }
        }

        // Patch BepInEx.Paths.BepInExRootPath to avoid missing SemanticVersioning.dll in headless tests
        var bepPathProp = typeof(BepInEx.Paths).GetProperty("BepInExRootPath", BindingFlags.Public | BindingFlags.Static);
        if (bepPathProp?.GetGetMethod() != null)
        {
            var prefixPath = typeof(SceneManagerGameManagerChallengerTests).GetMethod(nameof(PrefixBepInExRootPath), BindingFlags.NonPublic | BindingFlags.Static);
            try
            {
                harmony.Patch(bepPathProp.GetGetMethod(), new HarmonyMethod(prefixPath));
            }
            catch { }
        }
    }

    private static bool PrefixOpInequality(UnityEngine.Object x, UnityEngine.Object y, ref bool __result)
    {
        bool xNull = ReferenceEquals(x, null);
        bool yNull = ReferenceEquals(y, null);
        __result = (xNull != yNull) || (!xNull && !yNull && !ReferenceEquals(x, y));
        return false;
    }

    private static bool PrefixBepInExRootPath(ref string __result)
    {
        if (TestLoggerMock.OverrideBepInExRootPath != null)
        {
            __result = TestLoggerMock.OverrideBepInExRootPath;
            return false;
        }
        __result = @"C:\MockBepInEx";
        return false;
    }

    #region 1. SceneManager.Load with Null, Empty, Whitespace Strings

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n  \r")]
    public void SceneManager_Load_NullOrWhitespaceSceneName_LogsWarningAndDoesNotThrow(string invalidSceneName)
    {
        // Act
        var exception = Record.Exception(() => SceneManager.Load(invalidSceneName));

        // Assert
        Assert.Null(exception);
        Assert.Contains(TestLoggerMock.LoggedWarnings, msg =>
            msg.Contains("sceneName is null, empty, or whitespace", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SceneManager_Load_WhenUnderlyingLoadingScreenThrows_CatchesAndLogsError()
    {
        // Act: Loading a named scene in headless mode causes LoadingScreen proxy to throw
        var exception = Record.Exception(() => SceneManager.Load("NonExistentScene_12345"));

        // Assert: Exception is caught internally, never leaks to caller
        Assert.Null(exception);
        Assert.Contains(TestLoggerMock.LoggedErrors, msg =>
            msg.Contains("Failed to load scene 'NonExistentScene_12345'", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region 2. SceneManager.Load with Negative Indices

    [Theory]
    [InlineData(-1)]
    [InlineData(-999)]
    [InlineData(int.MinValue)]
    public void SceneManager_Load_NegativeSceneIndex_LogsWarningAndDoesNotThrow(int negativeIndex)
    {
        // Act
        var exception = Record.Exception(() => SceneManager.Load(negativeIndex));

        // Assert
        Assert.Null(exception);
        Assert.Contains(TestLoggerMock.LoggedWarnings, msg =>
            msg.Contains($"invalid sceneIndex '{negativeIndex}'", StringComparison.OrdinalIgnoreCase) &&
            msg.Contains("Index must be non-negative", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(999)]
    public void SceneManager_Load_ValidIndex_WhenLoadingScreenThrows_CatchesAndLogsError(int index)
    {
        // Act: Invoking with non-negative index in headless mode
        var exception = Record.Exception(() => SceneManager.Load(index));

        // Assert: Exception is caught internally, never leaks to caller
        Assert.Null(exception);
        Assert.Contains(TestLoggerMock.LoggedErrors, msg =>
            msg.Contains($"Failed to load scene at index {index}", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region 3. SceneManager.LoadModsBundle Double Invocation and Idempotency

    [Fact]
    public void SceneManager_LoadModsBundle_CalledTwiceInARow_DoesNotThrowOrCrash()
    {
        // Act - Call twice in a row
        var ex1 = Record.Exception(() => ReflectionHelper.InvokeLoadModsBundle());
        var ex2 = Record.Exception(() => ReflectionHelper.InvokeLoadModsBundle());

        // Assert
        // Neither call throws an unhandled exception out of LoadModsBundle
        Assert.Null(ex1);
        Assert.Null(ex2);

        // Verify that diagnostic error was safely logged indicating bundle file does not exist at mock path
        Assert.Contains(TestLoggerMock.LoggedErrors, msg =>
            msg.Contains("Mod bundle file does not exist at path", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SceneManager_LoadModsBundle_WhenAlreadyLoaded_LogsWarningAndSafelyNoOps()
    {
        var bundleField = typeof(SceneManager).GetField("_modsBundle", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(bundleField);

        var originalBundle = bundleField.GetValue(null);
        try
        {
            // Set dummy non-null object via reflection to simulate bundle already loaded
            // We use FormatterServices or an uninitialized AssetBundle if possible
            var dummyBundle = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(UnityEngine.AssetBundle));
            GC.SuppressFinalize(dummyBundle);
            bundleField.SetValue(null, dummyBundle);

            TestLoggerMock.ResetLogs();

            // Act
            ReflectionHelper.InvokeLoadModsBundle();

            // Assert
            Assert.Contains(TestLoggerMock.LoggedWarnings, msg =>
                msg.Contains("Mods Menu bundle is already loaded", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            // Restore original state
            bundleField.SetValue(null, originalBundle);
        }
    }

    #endregion

    #region 4. GameManager.RestartLevel When Scene Name Is Null, Empty, or MenuScene

    [Fact]
    public void GameManager_RestartLevel_WhenInMenuOrLoadingScene_ReturnsSafelyWithoutThrowing()
    {
        // In headless runner, Unity SceneManager returns default Scene struct where name is null/empty.
        // GameManager.RestartLevel must guard against null/empty scene name, MenuScene, and LoadingScene.
        var ex = Record.Exception(() => GameManager.RestartLevel());

        Assert.Null(ex);
        // Should return cleanly without throwing unhandled exceptions
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GameManager_RestartLevel_AcceptsPlayCutsceneParameter_WithoutThrowing(bool playCutscene)
    {
        var ex = Record.Exception(() => GameManager.RestartLevel(playCutscene));
        Assert.Null(ex);
    }

    #endregion

    #region 5. GameManager.RestartLevel DataManager and Key Validation Checks

    [Fact]
    public void GameManager_RestartLevel_MethodHasDefensiveTryCatchAndGuards()
    {
        var method = typeof(GameManager).GetMethod("RestartLevel", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        // Verify method can be invoked multiple times without throwing
        for (int i = 0; i < 5; i++)
        {
            var ex = Record.Exception(() => GameManager.RestartLevel(i % 2 == 0));
            Assert.Null(ex);
        }
    }

    [Fact]
    public void GameManager_Constants_ValidateKeysUsedForSaveDataLookup()
    {
        var type = typeof(GameManager);
        var keyField = type.GetField("gameManagerKey", BindingFlags.NonPublic | BindingFlags.Static);
        var idField = type.GetField("gameManagerId", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(keyField);
        Assert.NotNull(idField);

        string keyVal = (string)keyField.GetValue(null)!;
        string idVal = (string)idField.GetValue(null)!;

        // Verify non-empty valid GUID / identifier strings
        Assert.False(string.IsNullOrWhiteSpace(keyVal));
        Assert.False(string.IsNullOrWhiteSpace(idVal));
        Assert.True(Guid.TryParse(keyVal, out _), "gameManagerKey must be a valid GUID.");
        Assert.Equal("Deadshot.GameManager", idVal);
    }

    #endregion

    #region 6. GameManager.RestartLevel Null Singletons Guarding

    [Fact]
    public void GameManager_RestartLevel_SafeAgainstNullSingletons()
    {
        // When running in headless mode, DeadshotGameManager.INSTANCE and pauseMenu are null.
        // RestartLevel must not produce unhandled NullReferenceException.
        var ex = Record.Exception(() => GameManager.RestartLevel(true));
        Assert.Null(ex);
    }

    #endregion
}
