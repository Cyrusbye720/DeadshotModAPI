using System.IO;
using UnityEngine;
using Deadshot;

namespace DeadshotModAPI;

/// <summary>
/// Provides scene loading functionality for Deadshot mods.
/// </summary>
public static class SceneManager
{
    private static AssetBundle _modsBundle;

    /// <summary>
    /// Loads a scene by its name using Deadshot's built-in loading system.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    public static void Load(string sceneName)
    {
        LoadingScreen.LoadScene(sceneName);
    }

    /// <summary>
    /// Loads a scene by its build index using Deadshot's built-in loading system.
    /// </summary>
    /// <param name="sceneIndex">The build index of the scene to load.</param>
    public static void Load(int sceneIndex)
    {
        LoadingScreen.LoadScene(sceneIndex);
    }

    /// <summary>
    /// Loads the Deadshot Mod API's custom scene bundle. Broken Right now.
    /// </summary>
    internal static void LoadModsBundle()
    {
        string path = Path.Combine(
            BepInEx.Paths.BepInExRootPath,
            "mods",
            "DeadshotModAPI",
            "deadshotmodapi"
        );

        if (!File.Exists(path))
        {
            Logger.Error($"Mod bundle file does not exist at path: {path}");
            return;
        }

        _modsBundle = AssetBundle.LoadFromFile(path);

        if (_modsBundle == null)
        {
            Logger.Error($"Failed to load Mods Menu bundle: {path}");
            return;
        }

        Logger.Info("Mods Menu bundle loaded successfully.");
    }
}