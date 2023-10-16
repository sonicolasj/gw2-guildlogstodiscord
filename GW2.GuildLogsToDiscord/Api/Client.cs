using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace GW2.GuildLogsToDiscord.Api
{
    internal class Client : IDisposable
    {
        public Client(string apiKey)
        {
            _httpClient = new() { BaseAddress = new Uri(API_URL) };
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
        public async Task<IReadOnlyList<Guild>> GetAccountGuildsAsync()
        {
            var account = await _httpClient.GetFromJsonAsync<Account>("/v2/account");
            if (account is null) throw new Exception("Not found");

            var guildRequests = account.Guilds.Select(async id =>
            {
                var guild = await _httpClient.GetFromJsonAsync<Guild>($"/v2/guild/{id}");
                if (guild is null) throw new Exception("Not found");
                return guild;
            });

            var guilds = await Task.WhenAll(guildRequests);

            return guilds;
        }

        public async Task<IReadOnlyList<GuildLog>> GetGuildLogsAsync(Guid guildId)
        {
            var logs = await _httpClient.GetFromJsonAsync<JsonArray>($"/v2/guild/{guildId}/log");
            if (logs is null) throw new Exception("Not found");

            return logs.Select(node => GuildLog.Parse(node!)).ToList();
        }

        private readonly HttpClient _httpClient;
        public void Dispose() => _httpClient.Dispose();

        private const string API_URL = "https://api.guildwars2.com/";
    }

    internal record Account(IReadOnlyList<Guid> Guilds);
    internal record Guild(Guid Id, string Name, string Tag);

    internal abstract record GuildLog(uint Id, DateTime Time, string? User, string Type)
    {
        internal static GuildLog Parse(JsonNode json)
        {
            var type = (string)json["type"]!;

            return type switch
            {
                UsersAddedInfluenceLog.Type => UsersAddedInfluenceLog.Parse(json),
                UserJoinedLog.Type => UserJoinedLog.Parse(json),
                InvitedDeclinedLog.Type => InvitedDeclinedLog.Parse(json),
                UserInvitedLog.Type => UserInvitedLog.Parse(json),
                UserKickedLog.Type=> UserKickedLog.Parse(json),
                UserRankChangedLog.Type => UserRankChangedLog.Parse(json),
                UserAddedToTreasuryLog.Type => UserAddedToTreasuryLog.Parse(json),
                UserStashOperationLog.Type => UserStashOperationLog.Parse(json),
                UserChangedMOTDLog.Type => UserChangedMOTDLog.Parse(json),
                UserUpgradeActionLog.Type => UserUpgradeActionLog.Parse(json),

                _ => throw new NotSupportedException(),
            };
        }
    }

    // Note: Not documented in the wiki :/
    internal sealed record UsersAddedInfluenceLog(uint Id, DateTime Time, UsersAddedInfluenceLog.InfluenceActivity Activity, IReadOnlyList<string?> Users)
        : GuildLog(Id, Time, User: null, Type)
    {
        internal new static UsersAddedInfluenceLog Parse(JsonNode json)
        {
            return new UsersAddedInfluenceLog(
                Id: (uint)json["id"]!,
                Time: (DateTime)json["time"]!,
                Activity: Enum.Parse<InfluenceActivity>((string)json["activity"]!, ignoreCase: true),
                Users: (IReadOnlyList<string?>)json["users"]!
            );
        }

        internal new const string Type = "influence";

        internal enum InfluenceActivity { DailyLogin, Gifted }
    }

    internal sealed record UserJoinedLog(uint Id, DateTime Time, string User)
        : GuildLog(Id, Time, User, Type)
    {
        internal new static UserJoinedLog Parse(JsonNode json)
        {
            return new UserJoinedLog(
                Id: (uint)json["id"]!,
                Time: (DateTime)json["time"]!,
                User: (string)json["user"]!
            );
        }

        internal new const string Type = "joined";
    }

    // Note: Not documented in the wiki :/
    internal sealed record InvitedDeclinedLog(uint Id, DateTime Time, string User, string DeclinedBy)
        : GuildLog(Id, Time, User, Type)
    {
        internal new static InvitedDeclinedLog Parse(JsonNode json)
        {
            return new InvitedDeclinedLog(
                Id: (uint)json["id"]!,
                Time: (DateTime)json["time"]!,
                User: (string)json["user"]!,
                DeclinedBy: (string)json["declined_by"]!
            );
        }

        internal new const string Type = "invite_declined";
    }

    internal sealed record UserInvitedLog(uint Id, DateTime Time, string User, string InvitedBy)
        : GuildLog(Id, Time, User, Type)
    {
        internal new static UserInvitedLog Parse(JsonNode json)
        {
            return new UserInvitedLog(
                Id: (uint)json["id"]!,
                Time: (DateTime)json["time"]!,
                User: (string)json["user"]!,
                InvitedBy: (string)json["invited_by"]!
            );
        }

        internal new const string Type = "invited";
    }

    internal sealed record UserKickedLog(uint Id, DateTime Time, string User, string KickedBy)
        : GuildLog(Id, Time, User, Type)
    {
        internal new static UserKickedLog Parse(JsonNode json)
        {
            return new UserKickedLog(
                Id: (uint)json["id"]!,
                Time: (DateTime)json["time"]!,
                User: (string)json["user"]!,
                KickedBy: (string)json["kicked_by"]!
            );
        }

        internal new const string Type = "kick";
    }

    internal sealed record UserRankChangedLog(uint Id, DateTime Time, string User, string ChangedBy, string OldRank, string NewRank)
        : GuildLog(Id, Time, User, Type)
    {
        internal new static UserRankChangedLog Parse(JsonNode json)
        {
            return new UserRankChangedLog(
                Id: (uint)json["id"]!,
                Time: (DateTime)json["time"]!,
                User: (string)json["user"]!,
                ChangedBy: (string)json["changed_by"]!,
                OldRank: (string)json["old_rank"]!,
                NewRank: (string)json["new_rank"]!
            );
        }

        internal new const string Type = "rank_change";
    }

    internal sealed record UserAddedToTreasuryLog(uint Id, DateTime Time, string User, uint ItemId, uint Count)
        : GuildLog(Id, Time, User, Type)
    {
        internal new static UserAddedToTreasuryLog Parse(JsonNode json)
        {
            return new UserAddedToTreasuryLog(
                Id: (uint)json["id"]!,
                Time: (DateTime)json["time"]!,
                User: (string)json["user"]!,
                ItemId: (uint)json["item_id"]!,
                Count: (uint)json["count"]!
            );
        }

        internal new const string Type = "treasury";
    }

    internal sealed record UserStashOperationLog(uint Id, DateTime Time, string User, UserStashOperationLog.StashOperation Operation, uint ItemId, uint Count, uint Coins)
        : GuildLog(Id, Time, User, Type)
    {
        internal new static UserStashOperationLog Parse(JsonNode json)
        {
            return new UserStashOperationLog(
                Id: (uint)json["id"]!,
                Time: (DateTime)json["time"]!,
                User: (string)json["user"]!,
                Operation: Enum.Parse<StashOperation>((string)json["operation"]!, ignoreCase: true),
                ItemId: (uint)json["item_id"]!,
                Count: (uint)json["count"]!,
                Coins: (uint)json["coins"]!
            );
        }

        internal new const string Type = "stash";

        internal enum StashOperation { Deposit, Withdraw, Move }
    }

    internal sealed record UserChangedMOTDLog(uint Id, DateTime Time, string User, string MOTD)
        : GuildLog(Id, Time, User, Type)
    {
        internal new static UserChangedMOTDLog Parse(JsonNode json)
        {
            return new UserChangedMOTDLog(
                Id: (uint)json["id"]!,
                Time: (DateTime)json["time"]!,
                User: (string)json["user"]!,
                MOTD: (string)json["motd"]!
            );
        }

        internal new const string Type = "motd";
    }

    internal sealed record UserUpgradeActionLog(uint Id, DateTime Time, string? User, UserUpgradeActionLog.UpgradeAction Action, uint UpgradeId, uint? RecipeId, uint? Count)
        : GuildLog(Id, Time, User, Type)
    {
        internal new static UserUpgradeActionLog Parse(JsonNode json)
        {
            return new UserUpgradeActionLog(
                Id: (uint)json["id"]!,
                Time: (DateTime)json["time"]!,
                User: (string?)json["user"],
                Action: Enum.Parse<UpgradeAction>((string)json["action"]!, ignoreCase: true),
                UpgradeId: (uint)json["upgrade_id"]!,
                RecipeId: (uint?)json["recipe_id"],
                Count: (uint?)json["count"]
            );
        }

        internal new const string Type = "upgrade";

        internal enum UpgradeAction { Queued, Cancelled, Completed, SpedUp }
    }
}
