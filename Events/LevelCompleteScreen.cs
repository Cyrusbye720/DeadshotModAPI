public class LevelCompleteScreen
{
    private readonly Deadshot.UI.Menus.LevelCompleteMenu _menu;

    internal LevelCompleteScreen(
        Deadshot.UI.Menus.LevelCompleteMenu menu)
    {
        _menu = menu;
    }

    public void SetTime(string text)
    {
        _menu.timeText.text = $"{text}";
    }

    // TODO: Add the rest 
}