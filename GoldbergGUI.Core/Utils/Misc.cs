namespace GoldbergGUI.Core.Utils;

public class GlobalHelp
{
    public static string Header =>
        "Information";

    public static string TextPreLink =>
        "These settings are saved in";

    public static string Link => "app_config.json";

    public static string TextPostLink =>
        " adjacent to the application. " +
        "These are default values used for all games when configuring the Goldberg emulator. " +
        "The actual per-game configuration is saved in each game's steam_settings folder.";
}