# SpeedrunMod (Example Mod)

An example mod for Deadshot built with **DeadshotModAPI**.

It adds hotkeys to restart levels and skip cutscenes during speedrun attempts.

---

## Code

```csharp
using DeadshotModAPI;

public class SpeedrunMod : IDeadshotMod
{
    public string Name => "Deadshot Speedrunning";
    public string Description => "A Mod made for speedrunning Deadshot.";
    public string Creator => "Subaka";
    public string Version => "0.2.0";

    public void Load()
    {
        Logger.Debug("Deadshot Speedrun loaded through Mod API.");

        // F1: restart and skip cutscene
        // F2: restart with cutscene
        Input.OnKeyPressed(Key.F1, () => RestartLevelMethod(false));
        Input.OnKeyPressed(Key.F2, () => RestartLevelMethod(true));
    }

    private void RestartLevelMethod(bool playCutscene)
    {
        GameManager.RestartLevel(playCutscene);
    }
}
```

---

## Build

```bash
dotnet build ExampleMod.csproj -c Release
```

Output:
```text
bin/Release/net6.0/ExampleMod.dll
```

---

## Install

Copy `ExampleMod.dll` into `<Deadshot>/BepInEx/mods/`.

Launch Deadshot. The API loads the mod automatically.

Check `BepInEx/LogOutput.log` to confirm:
```text
[DeadshotModAPI] Loading mod: Deadshot Speedrunning
[DeadshotModAPI]   Description: A Mod made for speedrunning Deadshot.
[DeadshotModAPI]   Creator: Subaka
[DeadshotModAPI]   Version: 0.2.0
[DeadshotModAPI] Deadshot Speedrun loaded through Mod API.
```
