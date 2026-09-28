namespace DeadshotModAPI;

/// <summary>
/// Logging utility for Deadshot mods that routes messages through Unity and BepInEx.
/// </summary>
public static class Logger
{
    public static void Log(string message) => Info(message);

    public static void Info(string message)
    {
        UnityEngine.Debug.Log($"[DeadshotModAPI] {message}");
    }

    public static void Warning(string message)
    {
        UnityEngine.Debug.LogWarning($"[DeadshotModAPI] {message}");
    }

    public static void Error(string message)
    {
        UnityEngine.Debug.LogError($"[DeadshotModAPI] {message}");
    }

    public static void Debug(string message)
    {
        UnityEngine.Debug.Log($"[DeadshotModAPI] {message}");
    }
}