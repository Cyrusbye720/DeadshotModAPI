using System;
using System.IO;
using UnityEngine;
using Deadshot;

namespace DeadshotModAPI;

/// <summary>
/// Scene loading and asset bundle management for Deadshot mods.
/// </summary>
public static class SceneManager
{
    private static AssetBundle _modsBundle;

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
        if (_modsBundle != null)
        {
            Logger.Warning("Mods Menu bundle is already loaded.");
            return;
        }

        string rootPath;
        try
        {
            rootPath = BepInEx.Paths.BepInExRootPath;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to determine BepInEx root path: {ex}");
            return;
        }

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            Logger.Error("BepInEx root path is null or empty. Cannot discover mods.");
            return;
        }

        string path = Path.Combine(rootPath, "mods", "DeadshotModAPI", "deadshotmodapi");
        if (!File.Exists(path))
        {
            Logger.Error($"Mod bundle file does not exist at path: {path}");
            return;
        }

        try
        {
            _modsBundle = AssetBundle.LoadFromFile(path);
            if (_modsBundle == null)
            {
                Logger.Error($"Failed to load Mods Menu bundle: {path}");
                return;
            }

            Logger.Info("Mods Menu bundle loaded successfully.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load Mods Menu bundle from '{path}': {ex}");
        }
    }
}