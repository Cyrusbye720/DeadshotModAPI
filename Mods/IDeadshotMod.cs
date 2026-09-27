namespace DeadshotModAPI;

/// <summary>
/// Defines the required structure and metadata for a Deadshot mod.
/// </summary>
public interface IDeadshotMod
{
    /// <summary>
    /// Gets the display name of the mod.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a description of what the mod does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the name of the mod's creator.
    /// </summary>
    string Creator { get; }

    /// <summary>
    /// Gets the version of the mod.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Called when the mod is loaded by the Deadshot Mod API.
    /// </summary>
    void Load();
}