namespace DeadshotModAPI;

/// <summary>
/// Interface implemented by Deadshot mods.
/// </summary>
public interface IDeadshotMod
{
    /// <summary>
    /// Display name of the mod.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Short description of what the mod does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Name or handle of the author.
    /// </summary>
    string Creator { get; }

    /// <summary>
    /// Version of the mod (e.g. 1.0.0).
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Called when the mod is loaded by the API.
    /// </summary>
    void Load();
}