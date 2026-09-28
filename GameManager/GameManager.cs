using System;
using System.IO;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement;
using Deadshot.Data;
using Deadshot.Weapons;
using DeadshotGameManager = Deadshot.GameManager;

namespace DeadshotModAPI;

/// <summary>
/// Helpers for interacting with Deadshot's game state and level lifecycle.
/// </summary>
public class GameManager
{
    private const string gameManagerKey = "7069791d-77d0-45d8-8895-91b9fae46892";
    private const string gameManagerId = "Deadshot.GameManager";

    /// <summary>
    /// Restarts the active level.
    /// </summary>
    /// <param name="playCutscene">Whether to replay the opening cutscene upon restarting.</param>
    public static void RestartLevel(bool playCutscene = true)
    {
        try
        {
            var scene = UnitySceneManager.SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.name) || scene.name == "MenuScene" || scene.name == "LoadingScene")
            {
                return;
            }

            var file = DataManager.BACKUP_GAME_FILE_NAME;
            if (string.IsNullOrEmpty(file))
            {
                Logger.Error("Backup game file name is null or empty.");
                return;
            }

            var playerData = DataManager.LoadFile(file);
            if (playerData == null)
            {
                Logger.Error($"Failed to load player data from '{file}'.");
                return;
            }

            if (!playerData.ContainsKey(gameManagerKey))
            {
                Logger.Error($"Player data does not contain key '{gameManagerKey}'.");
                return;
            }

            var rawInner = playerData[gameManagerKey];
            if (rawInner == null)
            {
                Logger.Error($"Game manager dictionary is null for key '{gameManagerKey}'.");
                return;
            }

            var innerDict = rawInner.Cast<Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Object>>();
            if (innerDict == null)
            {
                Logger.Error("Failed to cast game manager data to dictionary.");
                return;
            }

            if (!innerDict.ContainsKey(gameManagerId))
            {
                Logger.Error($"Inner dictionary does not contain key '{gameManagerId}'.");
                return;
            }

            var rawData = innerDict[gameManagerId];
            if (rawData == null)
            {
                Logger.Error($"Game data is null for id '{gameManagerId}'.");
                return;
            }

            DeadshotGameManager.GameData data = rawData.Cast<DeadshotGameManager.GameData>();
            if (data == null)
            {
                Logger.Error("Failed to cast game data to DeadshotGameManager.GameData.");
                return;
            }

            data.isBeginingLevel = playCutscene;
            innerDict[gameManagerId] = data;

            playerData[gameManagerKey] = innerDict;

            string savePath = Path.Combine(
                DataManager.FILE_PATH,
                DataManager.BACKUP_GAME_FILE_NAME
            );
            DataManager.SaveFile(playerData, savePath);

            if (DeadshotGameManager.INSTANCE == null)
            {
                Logger.Error("Cannot restart level: DeadshotGameManager.INSTANCE is null.");
                return;
            }

            if (DeadshotGameManager.INSTANCE.pauseMenu == null)
            {
                Logger.Error("Cannot restart level: DeadshotGameManager.INSTANCE.pauseMenu is null.");
                return;
            }

            DeadshotGameManager.INSTANCE.pauseMenu.RestartLevel();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to restart level: {ex}");
        }
    }
}