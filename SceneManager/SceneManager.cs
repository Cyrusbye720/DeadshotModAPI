using System;
using System.IO;
using UnityEngine;
using Deadshot;
using BepInEx;
using System.Runtime.CompilerServices;
using UniverseLib;

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
    /// Loads the Deadshot Mod API UI asset bundle from BepInEx/mods/DeadshotModAPI.
    /// </summary>
    internal static void LoadModsBundle()
    {
        string path = Path.Combine(
            Paths.BepInExRootPath,
            "mods",
            "DeadshotModAPI",
            "deadshotmodapi"
        );

        if (!File.Exists(path))
        {
            Logger.Error($"Mod bundle does not exist: {path}");
            return;
        }

        try
        {
            Logger.Info($"Loading AssetBundle: {path}");

            UniverseLib.AssetBundle bundle = UniverseLib.AssetBundle.LoadFromFile(path, 0u, 0UL);

            if (bundle == null)
            {
                Logger.Error("UniverseLib failed to load the AssetBundle.");
                return;
            }

            Logger.Info($"Loaded AssetBundle: {bundle.name}");

            var assets = bundle.LoadAllAssets();

            Logger.Info($"Loaded {assets.Length} assets from bundle.");

            foreach (var asset in assets)
            {
                Logger.Info(
                    $"Asset: {asset.name} ({asset.GetType().FullName})"
                );
            }

            Logger.Info("Mods Menu bundle loaded successfully.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load Mods Menu bundle: {ex}");
        }
    }
}

// Wrote this calss before trying UniverseLib
// depending on if i can get UniversLib to work 
// this may get removed.
public class AssetBundleLoader
{
    public bool LoadAssetBundle(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"AssetBundle file not found at: {filePath}");
            }

            byte[] bundleBytes = LoadBundleToMemory(filePath);

            if (bundleBytes.Length == 0)
            {
                Logger.Error("AssetBundle file is empty.");
            }

            LoadBundleFromMemory(bundleBytes);
            return true;
        }
        catch   (Exception ex)
        {
            Logger.Error($"Failed to load asset bundle. {ex}");
            return false;
        }
    }
    
    internal static byte[] LoadBundleToMemory(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"AssetBundle file not found at: {filePath}");
                return null;
            }

            byte[] bundleBytes = File.ReadAllBytes(filePath);

            return bundleBytes;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load bundle to memory: {ex}");
            return null;
        }
    }

    internal static void LoadBundleFromMemory(byte[] bundleBytes)
    {
        try
        {
            UniverseLib.AssetBundle bundle = UniverseLib.AssetBundle.LoadFromMemory(bundleBytes);

            if (bundle == null)
            {
                Debug.LogError("Failed to load AssetBundle.");
                return;
            }
            Logger.Log($"Loaded AssetBundle: {bundle.name}");

            bundle.Unload(false);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load asset bundle from memory: {ex}");
        }
    }
}