using CounterStrikeSharp.API.Core;
using Menu.Enums;
using Menu;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Commands;
using System.Linq;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2AdminsList;

public class PluginConfig : BasePluginConfig
{
    public string ShowAdminsTo { get; set; } = "center";
    public string[] HideCommand { get; set; } = ["hide"];
    public string[] AdminsListCommand { get; set; } = ["admins", "adminslist", "adminsonline"];
    public string[] AdminFlag { get; set; } = ["@css/generic"];
    public bool ShowAdminGroup { get; set; } = true;
    public bool FreezePlayerInMenu { get; set; } = true;
    public bool DisableMenuCredits { get; set; } = true;
    public bool ShowYourSelf { get; set; } = true;
}

public class AdminsList : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[CS2] AdminsList";
    public override string ModuleVersion => "1.1";
    public PluginConfig Config { get; set; } = new();

    private readonly HashSet<ulong> hiddenAdmins = new();
    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        foreach (var cmd in Config.AdminsListCommand)
        {
            AddCommand($"css_{cmd}", "Shows Admins List", Command_AdminsList);
        }
        foreach (var cmd in Config.HideCommand)
        {
            AddCommand($"css_{cmd}", "Hide/UnHide from list", Command_Hide);
        }
    }

    private string FormatGroupName(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return string.Empty;

        string cleanedName = groupName.Replace("#", "").Trim();
        return char.ToUpper(cleanedName[0]) + cleanedName.Substring(1);
    }

    private string FormatGroupList(IEnumerable<string> groups)
    {
        return string.Join(", ", groups.Select(FormatGroupName));
    }

public void Command_AdminsList(CCSPlayerController? player, CommandInfo info)
{
    if (player == null || !player.IsValid || player.IsBot)
        return;

    // Verificare dacă are permisiunile de admin
    if (!AdminManager.PlayerHasPermissions(player, Config.AdminFlag))
    {
        player.PrintToChat(Localizer["prefix"] + "Nu ai permisiunea să folosești această comandă.");
        return;
    }

    var admins = Utilities.GetPlayers()
        .Where(p => p.IsValid && !p.IsBot)
        .Where(p => AdminManager.PlayerHasPermissions(p, Config.AdminFlag))
        .Where(p => !hiddenAdmins.Contains(p.SteamID))
        .ToList();

    if (!Config.ShowYourSelf)
    {
        admins.Remove(player);
    }

    if (admins.Count == 0)
    {
        switch (Config.ShowAdminsTo.ToLower())
        {
            case "center":
                ShowNoAdminsMenu(player);
                break;
            case "chat":
                player.PrintToChat(Localizer["prefix"] + Localizer["admin.empty.option.chat"]);
                break;
        }
        return;
    }

    switch (Config.ShowAdminsTo.ToLower())
    {
        case "center":
            ShowAdminsMenu(player, admins); 
            break;
        case "chat":
            ShowAdminsChat(player, admins);
            break;        
    }
}

    private void ShowAdminsChat(CCSPlayerController player, List<CCSPlayerController> admins)
    {
        player.PrintToChat(Localizer["admin.title.chat"]);
        player.PrintToChat($" {ChatColors.Red}====================");

        foreach (var admin in admins)
        {
            var adminData = AdminManager.GetPlayerAdminData(admin.AuthorizedSteamID);
            if (adminData == null)
                continue;

            string groupDisplay = Config.ShowAdminGroup && adminData.Groups.Count > 0
                ? $" [ {FormatGroupList(adminData.Groups)} ]"
                : string.Empty;
            player.PrintToChat($" {ChatColors.Red}{admin.PlayerName} {ChatColors.Olive}{groupDisplay}\n");
        }
        player.PrintToChat($" {ChatColors.Red}====================");
    }
    private void ShowAdminsMenu(CCSPlayerController player, List<CCSPlayerController> admins)
    {
        var menuTitle = Localizer["admin.title"];
        var menu = new KitsuneMenu(this);
        var items = new List<MenuItem>();

        foreach (var admin in admins)
        {
            var adminData = AdminManager.GetPlayerAdminData(admin.AuthorizedSteamID);
            if (adminData == null)
                continue;

            string groupDisplay = Config.ShowAdminGroup && adminData.Groups.Count > 0
                ? $" [ <font color=\"#00FF00\">{FormatGroupList(adminData.Groups)}</font> ]"
                : string.Empty;

            var menuText = $"<font color=\"#FFFF00\">{admin.PlayerName}</font>{groupDisplay}";

            items.Add(new MenuItem(MenuItemType.Text, new MenuValue(menuText)));
        }

        menu.ShowScrollableMenu(player, menuTitle, items, (buttons, currentMenu, selectedItems) =>
        {
            if (buttons == MenuButtons.Exit)
                return;

        }, isSubmenu:false, freezePlayer:Config.FreezePlayerInMenu, disableDeveloper: Config.DisableMenuCredits);
    }


    private void ShowNoAdminsMenu(CCSPlayerController player)
    {
        var menu = new KitsuneMenu(this);
        var empty = Localizer["admin.empty.option.center"];
        List<MenuItem> adminEmpty = new List<MenuItem>
        {
            new MenuItem(MenuItemType.Text, new MenuValue($"<font color=\"#FFFF00\">{empty}</font>"))
        };

        menu.ShowScrollableMenu(player, Localizer["admin.title"], adminEmpty, (menuButtons, currentMenu, selectedItem) =>
        {
            if (menuButtons == MenuButtons.Exit)
                return;

        }, isSubmenu:false, freezePlayer:Config.FreezePlayerInMenu, disableDeveloper: Config.DisableMenuCredits);
    }

    public void Command_Hide(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        ulong steamID = player.SteamID;

        if (hiddenAdmins.Contains(steamID))
        {
            hiddenAdmins.Remove(steamID);
            player.PrintToChat(Localizer["prefix"] + Localizer["admin.unhide"]);
        }
        else
        {
            hiddenAdmins.Add(steamID);
            player.PrintToChat(Localizer["prefix"] + Localizer["admin.hide"]);
        }
    }
}
