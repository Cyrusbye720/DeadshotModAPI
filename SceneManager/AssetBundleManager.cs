using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Deadshot.Player;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Microsoft.VisualBasic;
using UnityEngine;
using UniverseLib;

namespace DeadshotModAPI
{
    public class AssetBundleManager
    {
        private readonly string rootPath = Path.Combine(Paths.BepInExRootPath, "plugins/mods");

        public UniverseLib.AssetBundle LoadAssetBundle(string bundlesPath)
        {
            try
            {
                string path = Path.Combine(rootPath, bundlesPath);

                if (!File.Exists(path))
                {
                    Logger.Error($"AssetBundle file not found at: {path}");
                }

                var bundle = UniverseLib.AssetBundle.LoadFromFile(path);

                return bundle;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load asset bundle. {ex}");
                return null;
            }
        }

        public List<UniverseLib.AssetBundle> LoadAssetBundles(List<string> assetBundles)
        {
            try
            {
                List<UniverseLib.AssetBundle> bundles = new();

                foreach (string name in assetBundles)
                {
                    string path = Path.Combine(rootPath, name);

                    if (!File.Exists(path))
                    {
                        Logger.Error($"AssetBundle file not found at: {path}");
                        return new List<UniverseLib.AssetBundle>();
                    }

                    var bundle = UniverseLib.AssetBundle.LoadFromFile(path);

                    if (bundle == null)
                    {
                        Logger.Error($"Failed to load asset bundle: {path}");
                        return new List<UniverseLib.AssetBundle>();
                    }

                    bundles.Add(bundle);
                }

                return bundles;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load asset bundles. {ex}");
                return new List<UniverseLib.AssetBundle>();
            }
        }

        public static GameObject GetPlayerObject()
        {
            try
            {
                var player = Deadshot.GameManager.INSTANCE.PlayerManager.gameObject;

                Logger.Log($"Player: {player.name}");
                Logger.Log($"Scene: {player.scene.name}");
                Logger.Log($"Root: {player.transform.root.name}");

                return player;
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"[DeadshotModAPI] Failed to find player object: {ex}"
                );

                return null;
            }
        }

        public static void SpawnPlayerObject()
        {
            try
            {
                GameObject player = GetPlayerObject();

                if (player != null)
                {
                    try
                    {
                        UnityEngine.Object.Instantiate(player);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to spawn player object. {ex}");
                    }
                }

                Logger.Error("Failed to get player object.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to spawn player object. {ex}");
            }
        }
    }
}