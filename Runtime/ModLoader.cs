using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;
using UnityEngine;

namespace DeadshotModAPI;

/// <summary>
/// Discovers and loads mods implementing <see cref="IDeadshotMod"/> from BepInEx/mods.
/// </summary>
public class ModLoader : MonoBehaviour
{
    internal static readonly List<IDeadshotMod> LoadedMods = new();

    public void Awake()

    {
        Logger.Info("Deadshot Mod API ModLoader Awake.");
    }

    public void Start()
    {
        Logger.Info("Deadshot Mod API ModLoader Start.");
        try
        {
            LoadMods();
            SceneManager.LoadModsBundle();
        }
        catch (Exception ex)
        {
            Logger.Error($"Unexpected error during Start mod loading: {ex}");
        }
    }

    public void Update()
    {
        try
        {
            Input.CheckKeys();
        }
        catch (Exception ex)
        {
            Logger.Error($"Unexpected error in ModLoader.Update: {ex}");
        }
    }

    private static void LoadMods()
    {
        string rootPath;
        try
        {
            rootPath = Path.Combine(Paths.BepInExRootPath, "plugins");
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

        string modsPath;
        try
        {
            modsPath = Path.Combine(rootPath, "mods");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to construct mods directory path: {ex}");
            return;
        }

        try
        {
            if (!Directory.Exists(modsPath))
            {
                Directory.CreateDirectory(modsPath);
                Logger.Info($"Created mod directory: {modsPath}");
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to verify or create mod directory '{modsPath}': {ex}");
            return;
        }

        string[] modFiles;
        try
        {
            modFiles = Directory.GetFiles(modsPath, "*.dll", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to enumerate mod files in '{modsPath}': {ex}");
            return;
        }

        foreach (string file in modFiles)
        {
            LoadMod(file);
        }
    }

    private static void LoadMod(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Logger.Warning("Mod file path is null or empty.");
            return;
        }

        if (!File.Exists(path))
        {
            Logger.Error($"Mod file does not exist: {path}");
            return;
        }

        Assembly assembly = GetAssembly(path);

        if (assembly == null)
        {
            Logger.Warning($"Failed to load assembly: {Path.GetFileName(path)}");
            return;
        }

        Type[] types = GetTypes(assembly);

        if (types == null || types.Length == 0)
        {
            Logger.Warning($"No types found in assembly: {Path.GetFileName(path)}");
            return;
        }

        foreach (Type type in types.Where(t => t != null))
        {
            try
            {
                if (!typeof(IDeadshotMod).IsAssignableFrom(type) ||
                    type.IsInterface ||
                    type.IsAbstract)
                {
                    continue;
                }

                if (Activator.CreateInstance(type) is not IDeadshotMod mod)
                {
                    Logger.Warning($"Failed to instantiate mod '{type.FullName}' from '{path}'.");
                    continue;
                }

                string modName = "Unknown";
                try
                {
                    modName = mod.Name;
                }
                catch (Exception nameEx)
                {
                    Logger.Warning($"Failed to read mod name from '{type.FullName}': {nameEx.Message}");
                }

                Logger.Info($"Loading mod: {modName}");
                LogModMetaData(mod);

                mod.Load();
                LoadedMods.Add(mod);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to instantiate or load mod '{type?.FullName}' from '{Path.GetFileName(path)}': {ex}");
            }
        }
    }

    private static Assembly GetAssembly(string path)
    {
        try
        {
            return Assembly.LoadFrom(path);
        }
        catch (BadImageFormatException ex)
        {
            Logger.Error($"Corrupt or invalid mod assembly '{Path.GetFileName(path)}': {ex.Message}");
            return null;
        }
        catch (FileNotFoundException ex)
        {
            Logger.Error($"Mod assembly file not found '{path}': {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load assembly '{Path.GetFileName(path)}': {ex}");
            return null;
        }
    }

    private static Type[] GetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            if (ex.LoaderExceptions != null)
            {
                foreach (Exception loaderEx in ex.LoaderExceptions.Where(e => e != null))
                {
                    Logger.Warning($"  Loader exception: {loaderEx.Message}");
                }
            }
            return ex.Types;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to retrieve types from '{assembly.FullName}': {ex}");
            return Array.Empty<Type>();
        }
    }

    private static void LogModMetaData(IDeadshotMod mod)
    {
        try
        {
            Logger.Info($"  Name: {mod.Name}\n"
                + $"  Description: {mod.Description}\n"
                + $"  Creator: {mod.Creator}\n"
                + $"  Version: {mod.Version}");
        }
        catch (Exception ex)
        {
            Logger.Warning($"Failed to read mod metadata: {ex.Message}");
        }
    }
}