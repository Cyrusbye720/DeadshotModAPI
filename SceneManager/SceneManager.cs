using System;
using UnityEngine;
using Deadshot;
using System.Collections.Generic;

namespace DeadshotModAPI;

/// <summary>
/// Scene loading and asset bundle management for Deadshot mods.
/// </summary>
public static class SceneManager
{
    /// <summary>
    /// Loads a scene by its name using Deadshot's loading system.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    public static void Load(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Logger.Warning("Cannot load scene: sceneName is null, empty, or whitespace.");
            return;
        }

        try
        {
            LoadingScreen.LoadScene(sceneName);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load scene '{sceneName}': {ex}");
        }
    }

    /// <summary>
    /// Loads a scene by its build index using Deadshot's loading system.
    /// </summary>
    /// <param name="sceneIndex">The build index of the scene to load.</param>
    public static void Load(int sceneIndex)
    {
        if (sceneIndex < 0)
        {
            Logger.Warning($"Cannot load scene: invalid sceneIndex '{sceneIndex}'. Index must be non-negative.");
            return;
        }

        try
        {
            LoadingScreen.LoadScene(sceneIndex);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load scene at index {sceneIndex}: {ex}");
        }
    }

    /// <summary>
    /// Loads the Deadshot Mod API asset bundles from BepInEx/mods/DeadshotModAPI.
    /// </summary>
    /// <remarks>
    /// This is just a testing class to figure out loading assets.
    /// Need to move this to a seperate class.
    /// Add more functionality to this to give back to modders.
    /// </remarks>
    public static void LoadModsBundle()
    {
        try
        {
            AssetBundleManager bundleManager = new();
            List<string> bundlesFiles = new()
            {
                "DeadshotModApi/deadshotmodapi", // bundle 0
                "DeadshotModApi/deadshotapi_assets" // bundle 1
            };

            List<UniverseLib.AssetBundle> bundles = bundleManager.LoadAssetBundles(bundlesFiles);

            if (bundles == null)
            {
                Logger.Error("Failed to load asset bundles.");
            }

            Logger.Info($"Loaded {bundles.Count} asset bundles.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load Mods Menu bundle: {ex}");
        }
    }
}