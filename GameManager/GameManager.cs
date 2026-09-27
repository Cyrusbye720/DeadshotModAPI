using UnitySceneManager = UnityEngine.SceneManagement;
using Deadshot.Data;
using DeadshotGameManager = Deadshot.GameManager;
using System.IO;

namespace DeadshotModAPI;

public class GameManager
{
    private const string gameManagerKey = "7069791d-77d0-45d8-8895-91b9fae46892";
    private const string gameManagerId = "Deadshot.GameManager";

    /// <summary>
    /// Restarts the currently active level.
    /// </summary>
    /// <param name="playCutscene">
    /// Determines whether the level's introductory cutscene should play after restarting.
    /// </param>
    /// <remarks>
    /// The level will not be restarted if the current scene is the main menu
    /// or loading scene.
    /// </remarks>
    public static void RestartLevel(bool playCutscene = true)
    {
        var scene = UnitySceneManager.SceneManager.GetActiveScene();

        if (scene.name == "MenuScene" || scene.name == "LoadingScene")
        {
            return;
        }

        var file = DataManager.BACKUP_GAME_FILE_NAME;
        var playerData = DataManager.LoadFile(file);

        var innerDict =
            playerData[gameManagerKey]
                .Cast<Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Object>>();

        DeadshotGameManager.GameData data =
            innerDict[gameManagerId].Cast<DeadshotGameManager.GameData>();

        data.isBeginingLevel = playCutscene;
        innerDict[gameManagerId] = data;

        playerData[gameManagerKey] = innerDict;

        DataManager.SaveFile(
            playerData,
            Path.Combine(
                DataManager.FILE_PATH,
                DataManager.BACKUP_GAME_FILE_NAME
            )
        );

        DeadshotGameManager.INSTANCE.pauseMenu.RestartLevel();
    }
}