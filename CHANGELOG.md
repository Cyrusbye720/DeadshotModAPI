# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
## [1.0.0-dev-alpha] - 2026-09-28

### Added
- `EventManager.cs` for events.
- `LevelCompleteScreen.cs` for modifying `Deadshot.UI.Menus.LevelCompletedMenu`.
- `Harmony v2.4.2` for patching game instances.

### Changed
- Updated `Plugin.cs` version to `v1.0.0-dev-alpha`.
- Updated `Plugin.Load()` to add `EventManager` as component.
- Added null checks and `try/catch` exception handling to EventManager.

### Fixed
- Fixed runtime loading for `GameAssembly.dll` in `DeadshotModAPI.Tests.csproj`.
- Fixed git merge conflict errors with `origin/dev`.
- Fixed `LevelCompletedScreen.SetTime()` having a hardcoded text.
- Fixed spelling mistake in `Plugin.cs`

## [1.1.0] - 2026-09-28

### Added
- `Input.RemoveKeyPressed(Key, Action)` to unregister key callbacks.
- `examples/ExampleMod/` reference project featuring Subaka's `SpeedrunMod`.
- `DeadshotModAPI.Tests/` automated test suite covering mod discovery, reflection, and edge cases.
- `CONTRIBUTING.md` and `.github/pull_request_template.md`.

### Changed
- Updated `Plugin.cs` version to `1.1.0`.
- Converted all assembly references in `DeadshotModAPI.csproj` to relative paths (`lib\*.dll`).
- Renamed `Class1.cs` to `Plugin.cs`.
- Renamed `Logging/logger.cs` to `Logging/Logger.cs`.
- Upgraded GitHub Actions CI workflow to `actions/checkout@v4`.
- Rewrote `README.md` with clear setup instructions and API reference.

### Fixed
- Fixed hardcoded machine paths (`C:\Code\...`) and missing path separators in `DeadshotModAPI.csproj`.
- Added defensive null checks and `try/catch` isolation in `ModLoader` so corrupted or invalid mod DLLs log errors without crashing the game or stopping other mods.
- Added input validation to `SceneManager.Load` for null, whitespace, and negative indices.
- Added null guards in `GameManager.RestartLevel` before accessing game singletons and save data dictionaries.
- Fixed key collection mutation issue in `Input.CheckKeys` by snapshotting keys before dispatching callbacks.

## [1.0.0] - 2026-09-27

### Added
- Initial modding runtime for Deadshot on BepInEx 6 IL2CPP and .NET 6.
- `IDeadshotMod` interface.
- Dynamic mod loader scanning `BepInEx/mods/`.
- Keyboard input listening via Unity Input System.
- Level restart helper in `GameManager`.
- Scene loading helpers in `SceneManager`.
- Console logging via `Logger`.
