using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace DeadshotModAPI
{
    public class AssetBundleManager
    {
        private readonly string rootPath = Path.Combine(Paths.BepInExRootPath, "mods");

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
                        return null;
                    }

                    var bundle = UniverseLib.AssetBundle.LoadFromFile(path);

                    if (bundle == null)
                    {
                        Logger.Error($"Failed to load asset bundle: {path}");
                        return null;
                    }

                    bundles.Add(bundle);
                }

                return bundles;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load asset bundles. {ex}");
                return null;
            }
        }
    }
}