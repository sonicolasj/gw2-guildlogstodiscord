using System.Text.Json;
using GW2.GuildLogsToDiscord.Api;

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
    Console.WriteLine(log?.ToString() ?? "null");
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
        if (!File.Exists(filePath)) return null;

        var json = await File.ReadAllTextAsync(filePath);
        var settings = JsonSerializer.Deserialize<Settings>(json)!;

        if (string.IsNullOrWhiteSpace(settings.ApiKey)) return null;
        return settings;
    }

    public void Write(string filePath)
    {
        File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(this));
    }
}