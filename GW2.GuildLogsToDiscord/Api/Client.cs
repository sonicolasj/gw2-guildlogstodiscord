using Gw2Sharp;
using Gw2Sharp.WebApi.V2.Models;

namespace GW2.GuildLogsToDiscord.Api
{
    internal class Client : IDisposable
    {
        public Client(string apiKey)
        {
            _connection = new Connection(apiKey);
            _client = new Gw2Client(_connection);
        }

        public async Task<IReadOnlyList<Guild>> GetAccountGuildsAsync()
        {
            var account = await _client.WebApi.V2.Account.GetAsync();

            var guildRequests = account.GuildLeader.Select(async id =>
            {
                var guild = await _client.WebApi.V2.Guild[id].GetAsync();
                if (guild is null) throw new Exception("Not found");
                return guild;
            });

            var guilds = await Task.WhenAll(guildRequests);

            return guilds;
        }

        public async Task<IReadOnlyList<GuildLog>> GetGuildLogsAsync(Guid guildId)
        {
            var logs = await _client.WebApi.V2.Guild[guildId].Log.GetAsync();
            if (logs is null) throw new Exception("Not found");
            return logs;
        }

        private readonly Connection _connection;
        private readonly Gw2Client _client;
        public void Dispose() => _client.Dispose();
    }
}
