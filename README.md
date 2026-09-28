# Deadshot Mod API

[![Build](https://github.com/DemonZ-Development/DeadshotModAPI/actions/workflows/build.yml/badge.svg)](https://github.com/DemonZ-Development/DeadshotModAPI/actions/workflows/build.yml)
[![Framework](https://img.shields.io/badge/.NET-6.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/6.0)
[![Platform](https://img.shields.io/badge/BepInEx-6%20IL2CPP-blue.svg)](https://builds.bepinex.dev/projects/bepinex_be)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A modding library for **Deadshot** built on BepInEx 6 IL2CPP and .NET 6.

DeadshotModAPI handles mod discovery, assembly loading, key bindings, and scene loading so you can write mods without setting up BepInEx plugins or IL2CPP interop manually.

---

## Requirements

### Players
* **Deadshot**
* **BepInEx 6 (Bleeding Edge IL2CPP)**
* **`DeadshotModAPI.dll`** in `<GameDir>/BepInEx/plugins/`

### Mod Developers
* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
* A C# editor (Visual Studio, Rider, or VS Code)
* A reference to `DeadshotModAPI.dll` in your project

---

## Directory Layout

```text
Deadshot/
├── BepInEx/
│   ├── plugins/
│   │   └── DeadshotModAPI.dll       <-- API plugin
│   └── mods/
│       ├── SpeedrunMod.dll          <-- Your mod DLLs
│       └── CustomMod/
│           └── CustomMod.dll        <-- Subdirectories are scanned too
```

At startup, the API scans `BepInEx/mods/` recursively for DLLs, finds classes implementing `IDeadshotMod`, and calls their `Load()` method.

---

## Quickstart: Creating a Mod

1. Create a class library targeting .NET 6:
   ```bash
   dotnet new classlib -n SpeedrunMod -f net6.0
   ```
2. Reference `DeadshotModAPI.dll` in your `.csproj`:
   ```xml
   <ItemGroup>
     <Reference Include="DeadshotModAPI">
       <HintPath>path\to\DeadshotModAPI.dll</HintPath>
     </Reference>
   </ItemGroup>
   ```
3. Implement `IDeadshotMod`:
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

           Input.OnKeyPressed(Key.F1, () => RestartLevelMethod(false));
           Input.OnKeyPressed(Key.F2, () => RestartLevelMethod(true));
       }

       private void RestartLevelMethod(bool playCutscene)
       {
           GameManager.RestartLevel(playCutscene);
       }
   }
   ```
4. Build your mod:
   ```bash
   dotnet build -c Release
   ```
5. Copy the output DLL to `<Deadshot>/BepInEx/mods/`.

See [`examples/ExampleMod/`](examples/ExampleMod/) for a working project template.

---

## API Reference

### `IDeadshotMod`
Interface implemented by mods:
* `Name` - Mod display name.
* `Description` - Summary of mod behavior.
* `Creator` - Author name.
* `Version` - Version string (e.g. `"0.2.0"`).
* `Load()` - Runs once at game startup.

### `Input`
Keyboard listener:
* `Input.OnKeyPressed(Key key, Action callback)` - Registers a callback triggered when `key` is pressed.
* `Input.RemoveKeyPressed(Key key, Action callback)` - Unregisters a callback.

### `SceneManager`
Scene utilities:
* `SceneManager.Load(string sceneName)` - Loads a scene by name.
* `SceneManager.Load(int sceneIndex)` - Loads a scene by build index.

### `GameManager`
Level lifecycle utilities:
* `GameManager.RestartLevel(bool playCutscene = true)` - Restarts the active level. Set `playCutscene: false` to skip the intro cutscene.

### `Logger`
Unity and BepInEx console logger:
* `Logger.Info(string message)` / `Logger.Log(string message)`
* `Logger.Warning(string message)`
* `Logger.Error(string message)`
* `Logger.Debug(string message)`

---

## Building from Source

```bash
git clone https://github.com/DemonZ-Development/DeadshotModAPI.git
cd DeadshotModAPI
dotnet build DeadshotModAPI.csproj -c Release
dotnet test DeadshotModAPI.Tests/DeadshotModAPI.Tests.csproj
```

References point to `lib/` with relative paths, so the solution builds on any machine with the .NET 6 SDK.

---

## License

MIT. See [LICENSE](LICENSE).
