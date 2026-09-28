using DeadshotModAPI;

public class SpeedrunMod : IDeadshotMod
{
    public string Name => "Deadshot Speedrunning";
    public string Description => "A Mod made for speedrunning Deadshot.";
    public string Creator => "Subaka";
    public string Version => "0.2.0";

    public void Load()
    {
        Logger.Debug("Deadshot Speedrun loaded through Mod API.");

        Input.OnKeyPressed(Key.F1, () => RestartLevelMethod(false));
        Input.OnKeyPressed(Key.F2, () => RestartLevelMethod(true));
    }

    private void RestartLevelMethod(bool playCutscene)
    {
        GameManager.RestartLevel(playCutscene);
    }
}
