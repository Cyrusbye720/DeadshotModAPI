# Deadshot Mod API

[![Build](https://github.com/SniffBakaSniff/DeadshotModAPI/actions/workflows/build.yml/badge.svg)](https://github.com/SniffBakaSniff/DeadshotModAPI/actions/workflows/build.yml)

A modding API for **Deadshot** that is currently in development.

The goal of this project is to provide a common foundation for Deadshot mods, handling the BepInEx and Unity-side setup so individual mods can focus on their own functionality.

# Requirements

### For Players

* **Deadshot**
* **BepInEx 6 (Bleeding Edge)** installed and configured for Deadshot
* **Deadshot Mod API** installed in the game's `BepInEx/plugins` directory

### For Mod Developers

* **.NET 6**
* A C# development environment
* A reference to **`DeadshotModAPI.dll`** in the mod project

Mods do **not** need to be registered as BepInEx plugins. The API handles mod discovery and loading.

[BepInEx Bleeding Edge](https://builds.bepinex.dev/projects/bepinex_be)

## Mod Structure

Mods are loaded from:

```text
BepInEx/
├── plugins/
│   └── DeadshotModAPI.dll
│
└── mods/
    └── MyMod.dll
```

Mods must reference **`DeadshotModAPI.dll`** when being built. The API provides the interfaces and functionality that mods use at runtime.

A mod implements `IDeadshotMod` and provides its metadata and `Load()` method.

# Example Mod

A simple example demonstrating how to create a Deadshot mod using the Deadshot Mod API.

```csharp
using DeadshotModAPI;

public class ExampleMod : IDeadshotMod
{
    public string Name => "Example Mod";
    public string Description => "A simple example mod.";
    public string Creator => "Your Name";
    public string Version => "1.0.0";

    public void Load()
    {
        Input.OnKeyPressed(Key.F12, OnF12Pressed);

        Logger.Info("Example Mod loaded.");
    }

    private void OnF12Pressed()
    {
        Logger.Info("F12 was pressed!");
    }
}
```

The mod registers a callback for **F12** when it loads. When F12 is pressed, the API invokes `OnF12Pressed()`, which logs a message.

## Project Goals

The long-term goal is to make creating Deadshot mods as straightforward as possible while keeping game-specific and BepInEx-specific implementation details inside the API.

This project is experimental and should not currently be considered a stable modding API.
