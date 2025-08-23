using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace VoteExtend;

public class Config : BasePluginConfig
{
    public int MaximumExtends { get; set; } = 4;
    public int ExtendTime { get; set; } = 10;
    public float VoteTime { get; set; } = 30f;
    public float VotePassPercentage { get; set; } = .66f;
    public float ExtendDelay { get; set; } = 120f;
}

public class VoteExtend : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName { get; } = "Vote Extend";
    public override string ModuleVersion { get; } = "1.1";
    public override string ModuleAuthor { get; } = "Retro";
    public override string ModuleDescription { get; } = "Creates a vote to extend the current map";
    
    public void OnConfigParsed(Config config)
    {
        Config = config;
    }

    public Config Config { get; set; } = new();
    
    private int _totalExtends = 0;
    private Timer? _extendDelayTimer = null;
    private Timer? _voteTimer = null;
    private int _yesCount = 0;
    private int _noCount = 0;

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
    }

    private void OnMapStart(string mapname)
    {
        _totalExtends = 0;
        _voteTimer = null;
        _yesCount = 0;
        _noCount = 0;
        _extendDelayTimer = AddTimer(Config.ExtendDelay, () => { _extendDelayTimer = null; Server.PrintToChatAll($" {ChatColors.Magenta}[VE] {ChatColors.Default}You can now create a vote to extend the map");}, TimerFlags.STOP_ON_MAPCHANGE);
    }

    [ConsoleCommand("css_ve", "Creates a vote to extend the current map")]
    [RequiresPermissions("@css/reservation")]
    public void OnVoteExtendCommand(CCSPlayerController? controller, CommandInfo cmd)
    {
        if(_totalExtends >= Config.MaximumExtends)
        {
            cmd.ReplyToCommand($" {ChatColors.Magenta}[VE] {ChatColors.Default}The maximum amount of extends has already been reached");
            return;
        }

        if (_voteTimer is not null)
        {
            cmd.ReplyToCommand($" {ChatColors.Magenta}[VE] {ChatColors.Default}There is already an ongoing vote");
            return;
        }

        if (_extendDelayTimer is not null)
        {
            cmd.ReplyToCommand($" {ChatColors.Magenta}[VE] {ChatColors.Default}You must wait a bit before creating another vote");
            return;
        }

        _yesCount = 0;
        _noCount = 0;
        
        Server.PrintToChatAll($" {ChatColors.Magenta}[VE] {ChatColors.Green}{controller?.PlayerName ?? "Console"} {ChatColors.Default} has started a vote to extend the map.");

        foreach (var player in Utilities.GetPlayers().Where(con => !con.IsBot))
        {
            var menu = new ChatMenu("Vote Extend");
            menu.AddMenuOption("Yes", (voter, option) => { Server.PrintToChatAll($" {ChatColors.Magenta}[VE] {ChatColors.Green}{voter?.PlayerName ?? "Console"} {ChatColors.Default} has voted {ChatColors.Green}yes{ChatColors.Default}[{_yesCount}/{_yesCount+_noCount}].");
                _yesCount++; MenuManager.CloseActiveMenu(player); });
            menu.AddMenuOption("No", (voter, option) => { Server.PrintToChatAll($" {ChatColors.Magenta}[VE] {ChatColors.Green}{voter?.PlayerName ?? "Console"} {ChatColors.Default} has voted {ChatColors.LightRed}no{ChatColors.Default}[{_yesCount}/{_yesCount+_noCount}].");
                _noCount++; MenuManager.CloseActiveMenu(player); });
            MenuManager.OpenChatMenu(player, menu);
        }

        _voteTimer = AddTimer(Config.VoteTime, OnVoteFinished, TimerFlags.STOP_ON_MAPCHANGE);
    }

    private void OnVoteFinished()
    {
        _voteTimer = null;
        var totalVotes = _yesCount + _noCount;
        var percentage = (float) _yesCount / totalVotes;
        _extendDelayTimer = AddTimer(Config.ExtendDelay, () => { _extendDelayTimer = null; Server.PrintToChatAll($" {ChatColors.Magenta}[VE] {ChatColors.Default}You can now create a vote to extend the map");}, TimerFlags.STOP_ON_MAPCHANGE);

        foreach (var controller in Utilities.GetPlayers())
        {
            MenuManager.CloseActiveMenu(controller);
        }

        if (percentage >= Config.VotePassPercentage)
        {
            _totalExtends++;
            var timelimitConVar = ConVar.Find("mp_timelimit");
            var timeLimit = timelimitConVar?.GetPrimitiveValue<float>() ?? 20;
            var newTimeLimit = timeLimit + Config.ExtendTime;
            Server.PrintToChatAll($" {ChatColors.Magenta}[VE] {ChatColors.Default}The map has been extend by {Config.ExtendTime} minutes[{_totalExtends}/{Config.MaximumExtends}]");
            Server.ExecuteCommand($"mp_timelimit {newTimeLimit}");
        }
        else
        {
            Server.PrintToChatAll($" {ChatColors.Magenta}[VE] {ChatColors.Default}The vote to extend the map failed");
        }
    }
}