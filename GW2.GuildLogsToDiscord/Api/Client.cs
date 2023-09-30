using System.Net.Http.Json;

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
            var logs = await _httpClient.GetFromJsonAsync<IReadOnlyList<GuildLog>>($"/v2/guild/{guildId}/log");
            if (logs is null) throw new Exception("Not found");
            return logs;
        }

        private readonly HttpClient _httpClient;
        public void Dispose() => _httpClient.Dispose();

        private const string API_URL = "https://api.guildwars2.com/";
    }

    internal record Account(IReadOnlyList<Guid> Guilds);
    internal record Guild(Guid Id, string Name, string Tag);
    internal record GuildLog(int Id, DateTime Time, string? User, string Type);
}
