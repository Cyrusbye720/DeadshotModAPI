using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Deadshot.Player;

namespace DeadshotModAPI;

/// <summary>
/// Scene loading and asset bundle management for Deadshot mods.
/// </summary>
public static class SceneManager
{
    /// <summary>
    /// Loads a scene while keeping Deadshot's gameplay scene loaded
    /// so the existing player and gameplay systems remain available.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    public static void LoadScene(string sceneName)
    {
        Logger.Log($"LoadScene called with: '{sceneName}'");

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Logger.Error("LoadScene received an empty scene name.");
            return;
        }

        try
        {
            // -----------------------------------------------------
            // CHECK IF THIS SCENE IS ALREADY LOADED
            // -----------------------------------------------------

            UnityEngine.SceneManagement.Scene existingScene =
                default;

            for (int i = 0;
                 i < UnityEngine.SceneManagement.SceneManager.sceneCount;
                 i++)
            {
                UnityEngine.SceneManagement.Scene scene =
                    UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);

                if (scene.name == sceneName)
                {
                    existingScene = scene;
                    break;
                }
            }

            // -----------------------------------------------------
            // IF ALREADY LOADED, UNLOAD IT FIRST.
            //
            // This allows F6 to reload the custom scene instead
            // of creating another copy/waiter.
            // -----------------------------------------------------

            if (existingScene.IsValid() &&
                existingScene.isLoaded)
            {
                Logger.Log(
                    $"Scene '{sceneName}' is already loaded. " +
                    "Unloading before reloading."
                );

                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(
                    existingScene
                );

                // The next waiter will wait until the scene is gone
                // before loading it again.
            }

            // -----------------------------------------------------
            // LOAD C1L2 IF NEEDED
            // -----------------------------------------------------

            bool c1l2Loaded = false;

            for (int i = 0;
                 i < UnityEngine.SceneManagement.SceneManager.sceneCount;
                 i++)
            {
                UnityEngine.SceneManagement.Scene scene =
                    UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);

                if (scene.name == "C1L2" &&
                    scene.isLoaded)
                {
                    c1l2Loaded = true;
                    break;
                }
            }

            if (!c1l2Loaded)
            {
                Logger.Log(
                    "Loading Deadshot gameplay scene: C1L2"
                );

                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    "C1L2",
                    UnityEngine.SceneManagement.LoadSceneMode.Additive
                );

                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    "C1L2",
                    UnityEngine.SceneManagement.LoadSceneMode.Additive
                );
            }

            // -----------------------------------------------------
            // CREATE WAITER
            // -----------------------------------------------------

            var waiterObject =
                new GameObject("DeadshotModAPI_SceneLoadWaiter");

            UnityEngine.Object.DontDestroyOnLoad(
                waiterObject
            );

            SceneLoadWaiter waiter =
                waiterObject.AddComponent<SceneLoadWaiter>();

            waiter.Initialize(sceneName);
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Failed to load scene '{sceneName}': {ex}"
            );
        }
    }

    /// <summary>
    /// Loads the Deadshot Mod API asset bundles from
    /// BepInEx/mods/DeadshotModAPI.
    /// </summary>
    internal static void LoadModsBundle()
    {
        try
        {
            AssetBundleManager bundleManager = new();

            List<string> bundlesFiles = new()
            {
                "DeadshotModApi/deadshotmodapi",
                "DeadshotModApi/deadshotapi_assets"
            };

            List<UniverseLib.AssetBundle> bundles =
                bundleManager.LoadAssetBundles(bundlesFiles);

            if (bundles == null)
            {
                Logger.Error(
                    "Failed to load asset bundles."
                );

                return;
            }

            Logger.Info(
                $"Loaded {bundles.Count} asset bundles."
            );
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Failed to load Mods Menu bundle: {ex}"
            );
        }
    }
}

// TODO: Review ChatGPT Code for SceneLoadWaiter, then fix issues!
internal class SceneLoadWaiter : MonoBehaviour
{
    private string _sceneName = string.Empty;
    private GameObject _player;
    private bool _initialized;
    private bool _sceneLoadRequested;
    private bool _playerPlaced;
    private int _startupFrames;

    internal void Initialize(string sceneName)
    {
        Logger.Log(
            $"SceneLoadWaiter initialized with scene: '{sceneName}'"
        );

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Logger.Error(
                "SceneLoadWaiter received an empty scene name."
            );

            Destroy(gameObject);
            return;
        }

        _sceneName = sceneName;
    }

    internal void Update()
    {

        // ---------------------------------------------------------
        // WAIT FOR DEADSHOT TO FINISH INITIALIZING
        // ---------------------------------------------------------

        if (_startupFrames < 30)
        {
            _startupFrames++;
            return;
        }

        // ---------------------------------------------------------
        // INITIALIZE PLAYER
        // ---------------------------------------------------------

        if (!_initialized)
        {
            InitializePlayer();

            if (_player == null)
            {
                return;
            }

            _initialized = true;

            Logger.Log(
                "Player initialization complete."
            );
        }

        // ---------------------------------------------------------
        // FIND CUSTOM SCENE
        // ---------------------------------------------------------

        UnityEngine.SceneManagement.Scene customScene =
            default;

        for (int i = 0;
             i < UnityEngine.SceneManagement.SceneManager.sceneCount;
             i++)
        {
            UnityEngine.SceneManagement.Scene scene =
                UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);

            if (scene.name == _sceneName)
            {
                customScene = scene;
                break;
            }
        }

        // ---------------------------------------------------------
        // LOAD CUSTOM SCENE
        // ---------------------------------------------------------

        if (!customScene.IsValid() ||
            !customScene.isLoaded)
        {
            if (!_sceneLoadRequested)
            {
                _sceneLoadRequested = true;

                Logger.Log(
                    $"Loading custom scene: {_sceneName}"
                );

                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    _sceneName,
                    UnityEngine.SceneManagement.LoadSceneMode.Additive
                );
            }

            return;
        }

        // ---------------------------------------------------------
        // CUSTOM SCENE LOADED
        // ---------------------------------------------------------

        if (_playerPlaced)
        {
            return;
        }

        Logger.Log(
            $"Custom scene loaded: {_sceneName}"
        );

        GameObject spawnPoint =
            FindCustomPlayerSpawn();

        if (spawnPoint == null)
        {
            Logger.Error(
                "Could not find CustomPlayerSpawn."
            );

            return;
        }

        // ---------------------------------------------------------
        // TELEPORT
        // ---------------------------------------------------------

        CharacterController controller =
            _player.GetComponent<CharacterController>();

        if (controller == null)
        {
            Logger.Log("Failed to find player controller.");
            return;
        }

        controller.enabled = false;

        _player.transform.SetPositionAndRotation(
            spawnPoint.transform.position,
            spawnPoint.transform.rotation
        );

        if (controller != null)
        {
            controller.enabled = true;
        }

        Logger.Log(
            $"Moved player to CustomPlayerSpawn: " +
            $"{spawnPoint.transform.position}"
        );

        _playerPlaced = true;

        Logger.Log(
            $"Grounded={controller?.isGrounded} | " +
            $"Velocity={controller?.velocity} | " +
            $"Position={_player.transform.position}"
        );
    }

    private void InitializePlayer()
    {
        Camera _playerCamera;

        Deadshot.GameManager gameManager =
            Deadshot.GameManager.INSTANCE;

        if (gameManager == null)
        {
            return;
        }

        if (gameManager.PlayerManager == null)
        {
            return;
        }

        _player =
            gameManager.PlayerManager.gameObject;

        if (_player == null)
        {
            return;
        }

        Logger.Log(
            $"Found player: {_player.name}"
        );

        Transform playerCameraTransform =
            _player.transform.Find(
                "Head/MainCamera"
            );

        if (playerCameraTransform == null)
        {
            Logger.Error(
                "Could not find Player/Head/MainCamera."
            );

            return;
        }

        _playerCamera =
            playerCameraTransform.GetComponent<Camera>();

        if (_playerCamera == null)
        {
            Logger.Error(
                "Player MainCamera has no Camera component."
            );

            return;
        }

        // ---------------------------------------------------------
        // CAMERA
        // ---------------------------------------------------------

        foreach (Camera camera in
            UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
        {
            camera.enabled =
                camera == _playerCamera;
        }

        // ---------------------------------------------------------
        // HIDE MAIN MENU
        // ---------------------------------------------------------

        foreach (Canvas canvas in
            UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
        {
            if (canvas.gameObject.scene.name == "MainMenu")
            {
                canvas.enabled = false;
            }
        }

        // ---------------------------------------------------------
        // HIDE C1L1 VISUALS
        //
        // Keep ALL C1L1 COLLIDERS AND GAMEPLAY OBJECTS ALIVE.
        // ---------------------------------------------------------

        foreach (Renderer renderer in
            UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
        {
            if (renderer.gameObject.scene.name != "C1L1")
            {
                continue;
            }

            if (renderer.transform.IsChildOf(
                _player.transform))
            {
                continue;
            }

            renderer.enabled = false;
        }
    }

    private GameObject FindCustomPlayerSpawn()
    {
        UnityEngine.SceneManagement.Scene customScene =
            default;

        for (int i = 0;
             i < UnityEngine.SceneManagement.SceneManager.sceneCount;
             i++)
        {
            UnityEngine.SceneManagement.Scene scene =
                UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);

            if (scene.name == _sceneName)
            {
                customScene = scene;
                break;
            }
        }

        if (!customScene.IsValid() ||
            !customScene.isLoaded)
        {
            return null;
        }

        foreach (GameObject root in
            customScene.GetRootGameObjects())
        {
            GameObject spawn =
                FindChildRecursive(
                    root.transform,
                    "CustomPlayerSpawn"
                );

            if (spawn != null)
            {
                return spawn;
            }
        }

        return null;
    }

    private static GameObject FindChildRecursive(
        Transform parent,
        string name)
    {
        if (parent.name == name)
        {
            return parent.gameObject;
        }

        for (int i = 0;
             i < parent.childCount;
             i++)
        {
            Transform child =
                parent.GetChild(i);

            GameObject result =
                FindChildRecursive(
                    child,
                    name
                );

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}