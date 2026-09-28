using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace DeadshotModAPI;

[BepInPlugin("com.subaka.deadshotmodapi", "Deadshot Mod Api", "1.1.0")]
public class Plugin : BasePlugin
{
    public override void Load()
    {
        Logger.Info("Deadshot Mod API loaded.");
        AddComponent<ModLoader>();
    }
}