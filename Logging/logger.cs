namespace DeadshotModAPI;

/// <summary>
/// Provides logging functionality for Deadshot mods.
/// Messages are written through Unity's logging system and are displayed by BepInEx.
/// </summary>
public static class Logger
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Info(string message)
    {
        UnityEngine.Debug.Log($"[DeadshotModAPI] {message}");
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The warning message to log.</param>
    public static void Warning(string message)
    {
        UnityEngine.Debug.LogWarning($"[DeadshotModAPI] {message}");
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The error message to log.</param>
    public static void Error(string message)
    {
        UnityEngine.Debug.LogError($"[DeadshotModAPI] {message}");
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The debug message to log.</param>
    public static void Debug(string message)
    {
        UnityEngine.Debug.Log($"[DeadshotModAPI] {message}");
    }
}