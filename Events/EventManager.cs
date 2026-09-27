using System;
using DeadshotGameManager = Deadshot.GameManager;
using Deadshot.UI.Menus;
using HarmonyLib;

namespace DeadshotModAPI;

public static class EventManager
{
    /// <summary>
    /// Updates all events that require per-frame processing.
    /// </summary>
    public static void Update()
    {
        LevelEvent.Update();
    }

    public static class LevelEvent
    {
        private static float _lastLevelTime;

        /// <summary>
        /// Occurs whenever the current level playtime changes.
        /// </summary>
        public static event Action<float> LevelTimeChanged;

        /// <summary>
        /// Occurs when the level completion menu is enabled.
        /// </summary>
        public static event Action<LevelCompleteScreen> LevelCompleted;

        /// <summary>
        /// Updates level-related events that require per-frame processing.
        /// </summary>
        internal static void Update()
        {
            LevelPlaytimeEvent();
        }

        /// <summary>
        /// Checks the current level playtime and invokes
        /// <see cref="LevelTimeChanged"/> when the playtime has changed.
        /// </summary>
        internal static void LevelPlaytimeEvent()
        {
            if (DeadshotGameManager.INSTANCE == null)
                return;

            float levelTime = DeadshotGameManager.INSTANCE.CompletionTime;

            if (levelTime == _lastLevelTime)
                return;

            _lastLevelTime = levelTime;

            LevelTimeChanged?.Invoke(levelTime);
        }

        /// <summary>
        /// Gets the current levels playtime.
        /// </summary>
        /// <returns>The playtime as a float.</returns>
        public static float GetLevelPlaytime()
        {
            return DeadshotGameManager.INSTANCE.CompletionTime;
        }

        /// <summary>
        /// Invokes <see cref="LevelCompleted"/> when the game's
        /// level completion menu is enabled.
        /// </summary>
        internal static void LevelCompletedEvent(LevelCompleteScreen menu)
        {
            LevelCompleted?.Invoke(menu);
        }
    }

    /// <summary>
    /// Harmony patches used by the event manager.
    /// </summary>
    [HarmonyPatch(typeof(LevelCompleteMenu))]
    private static class LevelCompleteMenuPatch
    {
        /// <summary>
        /// Detects when the level completion menu is enabled.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(LevelCompleteMenu.OnEnable))]
        private static void OnEnable(LevelCompleteMenu __instance)
        {
            var screen = new LevelCompleteScreen(__instance);

            LevelEvent.LevelCompletedEvent(screen);
        }
    }
}