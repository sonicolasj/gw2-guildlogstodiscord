using System.Text.Json;
using GW2.GuildLogsToDiscord.Api;
using Gw2Sharp.WebApi.V2.Models;

const string SAVE_FILE_PATH = "./preferences.json";

var settings = await Settings.Read(SAVE_FILE_PATH);
if (settings is null)
{
    settings = new Settings(
        ApiKey: PromptForApiKey(),
        GuildId: null
    );
}

using var client = new Client(settings.ApiKey);

var guilds = await client.GetAccountGuildsAsync();
// Save the ApiKey now, as the request went through.
settings.Write(SAVE_FILE_PATH);

var selectedGuild = PromptForGuild(guilds, settings.GuildId);
settings = settings with { GuildId = selectedGuild.Id };

var logs = await client.GetGuildLogsAsync(selectedGuild.Id);
// Save the selectedGuild now, as the request went through.
settings.Write(SAVE_FILE_PATH);

foreach (var log in logs)
{
    Console.WriteLine(log.GetMessage());
}

static string PromptForApiKey()
{
    string? apiKey = default;

    do
    {
        Console.Write("Provide an API key:");
        var input = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(input))
        {
            apiKey = input;
        }
        else
        {
            Console.WriteLine("Invalid input");
        }
    } while (apiKey is null);

    return apiKey;
}

static Guild PromptForGuild(IReadOnlyList<Guild> guilds, Guid? preselectedGuildId)
{
    var preselectedGuild = guilds.FirstOrDefault(guild => guild.Id == preselectedGuildId);

    Console.WriteLine("Choose a guild:");

    for (var i = 0; i < guilds.Count; i++)
    {
        var guild = guilds[i];
        Console.Write($"{i + 1}: {guild.Name} [{guild.Tag}]");

        Console.WriteLine(
            (guild == preselectedGuild)
                ? " (default)"
                : ""
        );
    }

    Guild? selectedGuild = default;
    do
    {
        Console.Write("Your choice? ");

        var choice = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(choice) && preselectedGuild is not null)
        {
            return preselectedGuild;
        }

        if (int.TryParse(choice, out int index))
        {
            selectedGuild = guilds.ElementAtOrDefault(index - 1);
        }

        if (selectedGuild is null)
        {
            Console.WriteLine("Invalid choice");
        }
    } while (selectedGuild is null);

    return selectedGuild;
}

record Settings(string ApiKey, Guid? GuildId)
{
    public static async Task<Settings?> Read(string filePath)
    {
        if (!System.IO.File.Exists(filePath)) return null;

        var json = await System.IO.File.ReadAllTextAsync(filePath);
        var settings = JsonSerializer.Deserialize<Settings>(json)!;

        if (string.IsNullOrWhiteSpace(settings.ApiKey)) return null;
        return settings;
    }

    public void Write(string filePath)
    {
        System.IO.File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(this));
    }
}

static class LogExtensions
{
    internal static string GetMessage(this GuildLog log)
    {
        var message = log switch
        {
            GuildLogJoined l => $"{l.User} joined the guild",
            GuildLogInvited l => $"{l.InvitedBy} invited {l.User} in the guild",
            GuildLogInviteDeclined l => $"{l.User} declined the invitation",

            GuildLogKick l when l.KickedBy == l.User => $"{l.User} left the guild",
            GuildLogKick l => $"{l.KickedBy} kicked {l.User} from the guild",

            GuildLogRankChange l => $"{l.ChangedBy} changed the rank of {l.User} from {l.OldRank} to {l.NewRank}",
            GuildLogMotd l => $"{l.User} changed the MOTD to the following:\n{l.Motd}",

            GuildLogStash l when l.Operation.ToEnum() == GuildLogStashOperation.Deposit && l.Count > 0 => $"{l.User} deposited {l.Count} × item_id({l.ItemId}) in the guild stash",
            GuildLogStash l when l.Operation.ToEnum() == GuildLogStashOperation.Deposit && l.Count == 0 => $"{l.User} deposited {FormatCoins(l.Coins)} coins in the guild stash",

            GuildLogStash l when l.Operation.ToEnum() == GuildLogStashOperation.Move => $"{l.User} moved {l.Count} × item_id({l.ItemId}) in the guild stash",

            GuildLogStash l when l.Operation.ToEnum() == GuildLogStashOperation.Withdraw && l.Count > 0 => $"{l.User} withdrew {l.Count} × item_id({l.ItemId}) from the guild stash",
            GuildLogStash l when l.Operation.ToEnum() == GuildLogStashOperation.Withdraw && l.Count == 0 => $"{l.User} withdrew {FormatCoins(l.Coins)} from the guild stash",

            GuildLogTreasury l => $"{l.User} added {l.Count} × item_id({l.ItemId}) in the guild treasury",

            GuildLogUpgrade l when l.Action.ToEnum() == GuildLogUpgradeAction.Queued && l.User is null => $"upgrade_id({l.UpgradeId}) got queued",
            GuildLogUpgrade l when l.Action.ToEnum() == GuildLogUpgradeAction.Queued && l.User is not null => $"{l.User} queued upgrade_id({l.UpgradeId})",
            GuildLogUpgrade l when l.Action.ToEnum() == GuildLogUpgradeAction.Completed => $"{l.User} completed {l.Count} × item_id({l.ItemId})",
            GuildLogUpgrade l when l.Action.ToEnum() == GuildLogUpgradeAction.Cancelled => $"{l.User} cancelled {l.Count} × item_id({l.ItemId})",
            GuildLogUpgrade l when l.Action.ToEnum() == GuildLogUpgradeAction.SpedUp => $"{l.User} sped up {l.Count} × item_id({l.ItemId})",

            GuildLogInfluence l => $"{FormatUsers(l.Participants)} added influence to the guild",

            _ => JsonSerializer.Serialize(log)!,
        };

        return $"[{log.Time}]: {message}.";
    }

    private static string FormatCoins(int candidate)
    {
        if (candidate == 0) return "0c";
        var parts = new List<string>();

        var copper = candidate % 100;
        candidate = candidate / 100;
        parts.Add($"{copper}c");

        if (candidate != 0)
        {
            var silver = candidate % 100;
            candidate = candidate / 100;
            parts.Add($"{silver}s");
        }

        if (candidate != 0)
        {
            parts.Add($"{candidate}g");
        }

        parts.Reverse();
        return string.Join(" ", parts);
    }

    private static string FormatUsers(IReadOnlyList<string> candidate)
    {
        return candidate.ToArray() switch
        {
            [] => "None",
            [var user] => user,
            [var first, var second] => $"{first} and {second}",
            [.. var users, var last] => $"{string.Join(", ", users)}, and {last}",
        };
    }
}
