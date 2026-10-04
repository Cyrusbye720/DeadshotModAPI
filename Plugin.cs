using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace DeadshotModAPI;

[BepInPlugin("com.subaka.deadshotmodapi", "Deadshot Mod API", "v1.1.1-dev-alpha")]
public class Plugin : BasePlugin
{

    public override void Load()
    {
        Logger.Info("Deadshot Mod API loaded.");
        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<SceneLoadWaiter>())
        {
            ClassInjector.RegisterTypeInIl2Cpp<SceneLoadWaiter>();
        }
        var harmony = new Harmony("DeadshotModAPI");
        harmony.PatchAll();
        AddComponent<ModLoader>();
        AddComponent<EventManager>();
    }
}