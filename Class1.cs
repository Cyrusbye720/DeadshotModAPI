using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace DeadshotModAPI;

[BepInPlugin(
    "com.subaka.deadshotmodapi",
    "Deadshot Mod Api",
    "0.0.1"
)]
public class Plugin : BasePlugin
{
    private static Harmony _harmony;
    
    public override void Load()
    {
        Debug.Log("Deadshot Mod API loaded.");
        _harmony = new Harmony("DeadshotModAPI");
        _harmony.PatchAll();

        AddComponent<ModLoader>();
    }
}