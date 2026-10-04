using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadshotModAPI;

/// <summary>
/// Scene loading and asset bundle management for Deadshot mods.
/// </summary>
public static class SceneManager
{
    internal const string BaseGameplaySceneName = "C1L2";

    /// <summary>
    /// Loads a scene while keeping Deadshot's gameplay scene loaded
    /// so the existing player and gameplay systems remain available.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    public static void LoadScene(string sceneName)
    {
        Logger.Log($"LoadScene called with: '{sceneName}'");

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Logger.Error("LoadScene received an empty scene name.");
            return;
        }

        try
        {
            AsyncOperation unloadOperation = null;
            Scene existingScene = default;

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);

                if (scene.name == sceneName)
                {
                    existingScene = scene;
                    break;
                }
            }

            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                Logger.Log($"Scene '{sceneName}' is already loaded. Unloading before reloading.");
                unloadOperation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(existingScene);
            }

            bool baseGameplaySceneLoaded = false;

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);

                if (scene.name == BaseGameplaySceneName && scene.isLoaded)
                {
                    baseGameplaySceneLoaded = true;
                    break;
                }
            }

            if (!baseGameplaySceneLoaded)
            {
                Logger.Log($"Loading Deadshot gameplay scene: {BaseGameplaySceneName}");

                UnityEngine.SceneManagement.SceneManager.LoadScene(BaseGameplaySceneName, LoadSceneMode.Additive);
            }

            SceneLoadWaiter[] existingWaiters = UnityEngine.Object.FindObjectsByType<SceneLoadWaiter>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < existingWaiters.Length; i++)
            {
                UnityEngine.Object.Destroy(existingWaiters[i].gameObject);
            }

            var waiterObject = new GameObject("DeadshotModAPI_SceneLoadWaiter");
            UnityEngine.Object.DontDestroyOnLoad(waiterObject);
            SceneLoadWaiter waiter = waiterObject.AddComponent<SceneLoadWaiter>();
            waiter.Initialize(sceneName, unloadOperation);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load scene '{sceneName}': {ex}");
        }
    }

    /// <summary>
    /// Loads the Deadshot Mod API asset bundles from
    /// BepInEx/mods/DeadshotModAPI.
    /// </summary>
    internal static void LoadModsBundle()
    {
        try
        {
            AssetBundleManager bundleManager = new();

            List<string> bundlesFiles = new()
            {
                "DeadshotModApi/deadshotmodapi",
                "DeadshotModApi/deadshotapi_assets"
            };

            List<UniverseLib.AssetBundle> bundles = bundleManager.LoadAssetBundles(bundlesFiles);

            if (bundles == null)
            {
                Logger.Error("Failed to load asset bundles.");
                return;
            }

            Logger.Info($"Loaded {bundles.Count} asset bundles.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load Mods Menu bundle: {ex}");
        }
    }
}