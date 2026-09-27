using System;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace DeadshotModAPI;

/// <summary>
/// Responsible for discovering and loading Deadshot mods from the
/// <c>/BepInEx/mods/</c> directory and processing the API's input system.
/// </summary>
public class ModLoader : MonoBehaviour
{
    /// <summary>
    /// Called when the ModLoader component is initialized.
    /// </summary>
    public void Awake()
    {
        Debug.Log("Deadshot Mod API ModLoader Awake.");
    }

    /// <summary>
    /// Called when the ModLoader component starts.
    /// Loads all mods found in the <c>/BepInEx/mods/</c> directory.
    /// </summary>
    public void Start()
    {
        Debug.Log("Deadshot Mod API ModLoader Start.");

        // Commented out as it breaks stuff
        // SceneManager.LoadModsBundle(); 

        LoadMods();
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    public void Update()
    {
        Input.CheckKeys(); // Checks keyboard inputs
        EventManager.Update(); // Handles updating events
    }

    /// <summary>
    /// Scans the <c>/BepInEx/mods/</c> directory for DLL files
    /// and attempts to load each one as a Deadshot mod.
    /// </summary>
    private void LoadMods()
    {
        string modsPath = Path.Combine(Paths.BepInExRootPath, "mods");

        // Checks if the `/BepInEx/mods/` directory exists and if not then creates it.
        if (!Directory.Exists(modsPath))
        {
            Directory.CreateDirectory(modsPath);
            Debug.Log($"Created mod directory: {modsPath}");
            return;
        }

        foreach (string file in Directory.GetFiles(modsPath, "*.dll"))
        {
            LoadMod(file);
        }
    }

    /// <summary>
    /// Loads a mod assembly from the specified DLL and searches it for
    /// concrete implementations of <see cref="IDeadshotMod"/>.
    /// </summary>
    /// <param name="path">The file path of the mod DLL to load.</param>
    private void LoadMod(string path)
    {
        try
        {
            // Loads the dll file
            Assembly assembly = Assembly.LoadFrom(path);

            foreach (Type type in assembly.GetTypes())
            {
                // Checks whether the loaded class implements IDeadshotMod
                // and isn't an interface or abstract class.
                if (!typeof(IDeadshotMod).IsAssignableFrom(type) ||
                    type.IsInterface ||
                    type.IsAbstract)
                {
                    continue;
                }

                IDeadshotMod mod = (IDeadshotMod)Activator.CreateInstance(type)!;

                Debug.Log($"Loading mod: {mod.Name}");
                Debug.Log($"  Description: {mod.Description}");
                Debug.Log($"  Creator: {mod.Creator}");
                Debug.Log($"  Version: {mod.Version}");

                mod.Load();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load mod '{Path.GetFileName(path)}': {ex}");
        }
    }
}