﻿using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Server.Administration.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Maps;
using Content.Server.RoundEnd;
using Content.Shared.Administration.Managers;
using Content.Shared.CCVar;
using Content.Shared.Prototypes;
using Robust.Server.ServerStatus;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Administration;

public sealed class ServerApi : IPostInjectInit
{
    [Dependency] private readonly IStatusHost _statusHost = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!; // Players
    [Dependency] private readonly ISharedAdminManager _adminManager = default!; // Admins
    //[Dependency] private readonly GameTicker _gameTicker = default!; // Round ID and stuff
    [Dependency] private readonly IGameMapManager _gameMapManager = default!; // Map name
    //[Dependency] private readonly AdminSystem _adminSystem = default!; // Panic bunker
    //[Dependency] private readonly RoundEndSystem _roundEndSystem = default!; // Round API
    [Dependency] private readonly IServerNetManager _netManager = default!; // Kick
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!; // Game rules
    [Dependency] private readonly IComponentFactory _componentFactory = default!; // Needed to circumvent the "IoC has no context on this thread" error until I figure out how to do it properly
    [Dependency] private readonly ITaskManager _taskManager = default!; // game explodes when calling stuff from the non-game thread
    [Dependency] private readonly EntityManager _entityManager = default!;

    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    private string _token = default!;
    private ISawmill _sawmill = default!;
    private string _motd = default!;

    public void PostInject()
    {
        // Get
        _statusHost.AddHandler(InfoHandler);
        _statusHost.AddHandler(GetGameRules);
        _statusHost.AddHandler(GetForcePresets);

        // Post
        _statusHost.AddHandler(ActionRoundStatus);
        _statusHost.AddHandler(ActionKick);
        _statusHost.AddHandler(ActionAddGameRule);
        _statusHost.AddHandler(ActionEndGameRule);
        _statusHost.AddHandler(ActionForcePreset);
        _statusHost.AddHandler(ActionForceMotd);
        _statusHost.AddHandler(ActionPanicPunker);

        _config.OnValueChanged(CCVars.AdminApiToken, UpdateToken, true);
        _config.OnValueChanged(CCVars.MOTD, UpdateMotd, true);

        _sawmill = Logger.GetSawmill("serverApi");
    }

    public void Shutdown()
    {
        _config.UnsubValueChanged(CCVars.AdminApiToken, UpdateToken);
        _config.UnsubValueChanged(CCVars.MOTD, UpdateMotd);
    }

    private void UpdateToken(string token)
    {
        _token = token;
    }

    private void UpdateMotd(string motd)
    {
        _motd = motd;
    }


#region Actions

    /// <summary>
    ///     Changes the panic bunker settings.
    /// </summary>
    private async Task<bool> ActionPanicPunker(IStatusHandlerContext context)
    {

        if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/admin/actions/panic_bunker")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var actionSupplied = context.RequestHeaders.TryGetValue("Action", out var action);
        if (!actionSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        var valueSupplied = context.RequestHeaders.TryGetValue("Value", out var value);
        if (!valueSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        switch (action) // TODO: This looks bad, there has to be a better way to do this.
        {
            case "enabled":
                if (!bool.TryParse(value.ToString(), out var enabled))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    _config.SetCVar(CCVars.PanicBunkerEnabled, enabled);
                });
                break;
            case "disable_with_admins":
                if (!bool.TryParse(value.ToString(), out var disableWithAdmins))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    _config.SetCVar(CCVars.PanicBunkerDisableWithAdmins, disableWithAdmins);
                });
                break;
            case "enable_without_admins":
                if (!bool.TryParse(value.ToString(), out var enableWithoutAdmins))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    _config.SetCVar(CCVars.PanicBunkerEnableWithoutAdmins, enableWithoutAdmins);
                });
                break;
            case "count_deadminned_admins":
                if (!bool.TryParse(value.ToString(), out var countDeadminnedAdmins))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    _config.SetCVar(CCVars.PanicBunkerCountDeadminnedAdmins, countDeadminnedAdmins);
                });
                break;
            case "show_reason":
                if (!bool.TryParse(value.ToString(), out var showReason))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    _config.SetCVar(CCVars.PanicBunkerShowReason, showReason);
                });
                break;
            case "min_account_age_hours":
                if (!int.TryParse(value.ToString(), out var minAccountAgeHours))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    _config.SetCVar(CCVars.PanicBunkerMinAccountAge, minAccountAgeHours * 60);
                });
                break;
            case "min_overall_hours":
                if (!int.TryParse(value.ToString(), out var minOverallHours))
                {
                    await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                    return true;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    _config.SetCVar(CCVars.PanicBunkerMinOverallHours, minOverallHours * 60);
                });
                break;
        }

        await context.RespondAsync("Success", HttpStatusCode.OK);
        return true;
    }

    /// <summary>
    ///     Sets the current MOTD.
    /// </summary>
    private async Task<bool> ActionForceMotd(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/admin/actions/set_motd")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var motdSupplied = context.RequestHeaders.TryGetValue("MOTD", out var motd);
        if (!motdSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        _config.SetCVar(CCVars.MOTD, motd.ToString()); // A hook in the MOTD system sends the changes to each client
        await context.RespondAsync("Success", HttpStatusCode.OK);
        return true;
    }

    /// <summary>
    ///     Forces the next preset-
    /// </summary>
    private async Task<bool> ActionForcePreset(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/admin/actions/force_preset")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var ticker = _entitySystemManager.GetEntitySystem<GameTicker>();

        if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            await context.RespondAsync("This can only be executed while the game is in the pre-round lobby.", HttpStatusCode.BadRequest);
            return true;
        }

        var presetSupplied = context.RequestHeaders.TryGetValue("PresetId", out var preset);
        if (!presetSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        var result = await RunOnMainThread(() => ticker.FindGamePreset(preset.ToString()));
        if (result == null)
        {
            await context.RespondAsync($"No preset exists with name {preset}.", HttpStatusCode.NotFound);
            return true;
        }

        _taskManager.RunOnMainThread(() =>
        {
            ticker.SetGamePreset(result);
        });
        _sawmill.Info($"Forced the game to start with preset {preset}.");
        await context.RespondAsync("Success", HttpStatusCode.OK);
        return true;
    }

    /// <summary>
    ///     Ends an active game rule.
    /// </summary>
    private async Task<bool> ActionEndGameRule(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/admin/actions/end_game_rule")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var gameRuleSupplied = context.RequestHeaders.TryGetValue("GameRuleId", out var gameRule);
        if (!gameRuleSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }
        var ticker = _entitySystemManager.GetEntitySystem<GameTicker>();
        var gameRuleEntity = await RunOnMainThread(() => ticker
            .GetActiveGameRules()
            .FirstOrNull(rule => _entityManager.MetaQuery.GetComponent(rule).EntityPrototype?.ID == gameRule.ToString()));

        if (gameRuleEntity == null) // Game rule not found
        {
            await context.RespondAsync("Gamerule not found or not active",HttpStatusCode.NotFound);
            return true;
        }

        ticker.EndGameRule((EntityUid) gameRuleEntity);
        await context.RespondAsync($"Ended game rule {gameRuleEntity}", HttpStatusCode.OK);
        return true;
    }

    /// <summary>
    ///     Adds a game rule to the current round.
    /// </summary>
    private async Task<bool> ActionAddGameRule(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/admin/actions/add_game_rule")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var gameRuleSupplied = context.RequestHeaders.TryGetValue("GameRuleId", out var gameRule);
        if (!gameRuleSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        var ticker = _entitySystemManager.GetEntitySystem<GameTicker>();

        var tsc = new TaskCompletionSource<EntityUid>();
        _taskManager.RunOnMainThread(() =>
        {
            var ruleEntity = ticker.AddGameRule(gameRule.ToString());
            if (ticker.RunLevel == GameRunLevel.InRound)
            {
                ticker.StartGameRule(ruleEntity);
            }
            tsc.TrySetResult(ruleEntity);
        });

        var ruleEntity = await tsc.Task;

        await context.RespondAsync($"Added game rule {ruleEntity}", HttpStatusCode.OK);
        return true;
    }

    /// <summary>
    ///     Kicks a player.
    /// </summary>
    private async Task<bool> ActionKick(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/admin/actions/kick")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var playerSupplied = context.RequestHeaders.TryGetValue("Username", out var username);
        if (!playerSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        var session = await RunOnMainThread(() =>
        {
            _playerManager.TryGetSessionByUsername(username.ToString(), out var session);
            return session;
        });

        if (session == null)
        {
            await context.RespondAsync("Player not found", HttpStatusCode.NotFound);
            return true;
        }

        var reasonSupplied = context.RequestHeaders.TryGetValue("Reason", out var reason);
        if (!reasonSupplied)
        {
            reason = "No reason supplied";
        }

        reason += " (kicked by admin)";

        _taskManager.RunOnMainThread(() =>
        {
            _netManager.DisconnectChannel(session.Channel, reason.ToString());
        });
        await context.RespondAsync("Success", HttpStatusCode.OK);
        _sawmill.Info("Kicked player {0} ({1})", username, reason);
        return true;
    }

    /// <summary>
    ///     Round restart/end
    /// </summary>
    private async Task<bool> ActionRoundStatus(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Post || context.Url!.AbsolutePath != "/admin/actions/round")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        // Not using body, because that's a stream and I don't want to deal with that
        var actionSupplied = context.RequestHeaders.TryGetValue("Action", out var action);
        if (!actionSupplied)
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return true;
        }

        var ticker = _entitySystemManager.GetEntitySystem<GameTicker>();
        var roundEndSystem = _entitySystemManager.GetEntitySystem<RoundEndSystem>();
        switch (action)
        {
            case "start":
                if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
                {
                    await context.RespondAsync("Round already started", HttpStatusCode.BadRequest);
                    _sawmill.Info("Forced round start failed: round already started");
                    return true;
                }
                _taskManager.RunOnMainThread(() =>
                {
                    ticker.StartRound();
                });
                _sawmill.Info("Forced round start");
                break;
            case "end":
                if (ticker.RunLevel != GameRunLevel.InRound)
                {
                    await context.RespondAsync("Round already ended", HttpStatusCode.BadRequest);
                    _sawmill.Info("Forced round end failed: round is not in progress");
                    return true;
                }
                _taskManager.RunOnMainThread(() =>
                {
                    roundEndSystem.EndRound();
                });
                _sawmill.Info("Forced round end");
                break;
            case "restart":
                if (ticker.RunLevel != GameRunLevel.InRound)
                {
                    await context.RespondAsync("Round already ended", HttpStatusCode.BadRequest);
                    _sawmill.Info("Forced round restart failed: round is not in progress");
                    return true;
                }
                _taskManager.RunOnMainThread(() =>
                {
                    roundEndSystem.EndRound();
                });
                _sawmill.Info("Forced round restart");
                break;
            case "restartnow": // You should restart yourself NOW!!!
                _taskManager.RunOnMainThread(() =>
                {
                    ticker.RestartRound();
                });
                _sawmill.Info("Forced instant round restart");
                break;
            default:
                await context.RespondErrorAsync(HttpStatusCode.BadRequest);
                return true;
        }

        await context.RespondAsync("Success", HttpStatusCode.OK);
        return true;
    }
#endregion

#region Fetching

    /// <summary>
    ///     Returns an array containing all presets.
    /// </summary>
    private async Task<bool> GetForcePresets(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Get || context.Url!.AbsolutePath != "/admin/force_presets")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var jObject = new JsonObject();
        var presets = new List<string>();
        foreach (var preset in _prototypeManager.EnumeratePrototypes<GamePresetPrototype>())
        {
            presets.Add(preset.ID);
        }

        jObject["presets"] = JsonNode.Parse(JsonSerializer.Serialize(presets));
        await context.RespondAsync(jObject.ToString(), HttpStatusCode.OK);
        return true;
    }

    /// <summary>
    ///    Returns an array containing all game rules.
    /// </summary>
    private async Task<bool> GetGameRules(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Get || context.Url!.AbsolutePath != "/admin/game_rules")
        {
            return false;
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        var jObject = new JsonObject();
        var gameRules = new List<string>();
        foreach (var gameRule in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (gameRule.Abstract)
                continue;

            if (gameRule.HasComponent<GameRuleComponent>(_componentFactory))
                gameRules.Add(gameRule.ID);
        }

        jObject["game_rules"] = JsonNode.Parse(JsonSerializer.Serialize(gameRules));
        await context.RespondAsync(jObject.ToString(), HttpStatusCode.OK);
        return true;
    }


    /// <summary>
    ///     Handles fetching information.
    /// </summary>
    private async Task<bool> InfoHandler(IStatusHandlerContext context)
    {
        if (context.RequestMethod != HttpMethod.Get || context.Url!.AbsolutePath != "/admin/info" || _token == string.Empty)
        {
            return false;
            // 404
        }

        if (!CheckAccess(context))
        {
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        /*  Information to display
            Round number
            Connected players
            Active admins
            Active game rules
            Active game preset
            Active map
            MOTD
            Panic bunker status
         */

        var ticker = _entitySystemManager.GetEntitySystem<GameTicker>();
        var adminSystem = _entitySystemManager.GetEntitySystem<AdminSystem>();

        var jObject = new JsonObject();

        jObject["round_id"] = await RunOnMainThread(() => ticker.RoundId);

        var players = new List<Player>();
        var onlineAdmins = new List<Player>();
        var onlineAdminsDeadmined = new List<Player>();

        foreach (var player in _playerManager.Sessions)
        {
            players.Add(new Player
            {
                Guid = player.UserId.UserId.ToString(),
                Name = player.Name
            });
            if (await RunOnMainThread(() => _adminManager.IsAdmin(player)))
            {
                onlineAdmins.Add(new Player
                {
                    Guid = player.UserId.UserId.ToString(),
                    Name = player.Name
                });
            }
            else if (await RunOnMainThread(() => _adminManager.IsAdmin(player, true)))
            {
                onlineAdminsDeadmined.Add(new Player
                {
                    Guid = player.UserId.UserId.ToString(),
                    Name = player.Name
                });
            }
        }

        // The JsonSerializer.Serialize into JsonNode.Parse is a bit of a hack
        jObject["players"] = JsonNode.Parse(JsonSerializer.Serialize(players));
        jObject["admins"] = JsonNode.Parse(JsonSerializer.Serialize(onlineAdmins));
        jObject["deadmined"] = JsonNode.Parse(JsonSerializer.Serialize(onlineAdminsDeadmined));

        var gameRules = new List<string>();
        foreach (var addedGameRule in await RunOnMainThread(() => ticker.GetActiveGameRules()))
        {
            var meta = _entityManager.MetaQuery.GetComponent(addedGameRule);
            gameRules.Add(meta.EntityPrototype?.ID ?? meta.EntityPrototype?.Name ?? "Unknown");
        }

        jObject["game_rules"] = JsonNode.Parse(JsonSerializer.Serialize(gameRules));
        jObject["game_preset"] = ticker.CurrentPreset?.ID;
        jObject["map"] = await RunOnMainThread(() => _gameMapManager.GetSelectedMap()?.MapName ?? "Unknown");
        jObject["motd"] = _motd;
        jObject["panic_bunker"] = new JsonObject();
        jObject["panic_bunker"]!["enabled"] = adminSystem.PanicBunker.Enabled;
        jObject["panic_bunker"]!["disable_with_admins"] = adminSystem.PanicBunker.DisableWithAdmins;
        jObject["panic_bunker"]!["enable_without_admins"] = adminSystem.PanicBunker.EnableWithoutAdmins;
        jObject["panic_bunker"]!["count_deadminned_admins"] = adminSystem.PanicBunker.CountDeadminnedAdmins;
        jObject["panic_bunker"]!["show_reason"] = adminSystem.PanicBunker.ShowReason;
        jObject["panic_bunker"]!["min_account_age_hours"] = adminSystem.PanicBunker.MinAccountAgeHours;
        jObject["panic_bunker"]!["min_overall_hours"] = adminSystem.PanicBunker.MinOverallHours;

        await context.RespondAsync(jObject.ToString(), HttpStatusCode.OK);
        return true;
    }

#endregion

    private bool CheckAccess(IStatusHandlerContext context)
    {
        var auth = context.RequestHeaders.TryGetValue("Authorization", out var authToken);
        if (!auth)
        {
            _sawmill.Info(@"Unauthorized access attempt to admin API. No auth header");
            return false;
        } // No auth header, no access

        if (authToken == _token)
            return true;

        // Invalid auth header, no access
        _sawmill.Info(@"Unauthorized access attempt to admin API. ""{0}"" vs ""{1}""", authToken, _token);
        return false;
    }

    /// <summary>
    /// Async helper function which runs a task on the main thread and returns the result.
    /// </summary>
    private async Task<T> RunOnMainThread<T>(Func<T> func)
    {
        var taskCompletionSource = new TaskCompletionSource<T>();
        _taskManager.RunOnMainThread(() =>
        {
            taskCompletionSource.TrySetResult(func());
        });

        var result = await taskCompletionSource.Task;
        return result;
    }

    private sealed class Player
    {
        public string Guid { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
}
