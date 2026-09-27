using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace DeadshotModAPI;

[BepInPlugin(
    "com.subaka.deadshotmodapi",
    "Deadshot Mod Api",
    "0.0.1"
)]
public class Plugin : BasePlugin
{
    public override void Load()
    {
        Debug.Log("Deadshot Mod API loaded.");

        AddComponent<ModLoader>();
    }
}