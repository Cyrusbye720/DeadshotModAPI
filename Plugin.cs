using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace DeadshotModAPI;

[BepInPlugin("com.subaka.deadshotmodapi", "Deadshot Mod Api", "v1.0.0-dev-alpha")]
public class Plugin : BasePlugin
{
    private static Harmony _harmony;
    
    public override void Load()
    {
        Logger.Info("Deadshot Mod API loaded.");
        _harmony = new Harmony("DeadshotModAPI");
        _harmony.PatchAll();
        AddComponent<ModLoader>();
    }
}