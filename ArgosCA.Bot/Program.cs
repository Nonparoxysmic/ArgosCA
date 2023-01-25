using ArgosCA.Bot;
using ArgosCA.Bot.Commands;
using Remora.Commands.Extensions;
using Remora.Discord.API;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Hosting.Extensions;
using Remora.Rest.Core;
using Remora.Results;

// Create application configuration.
//     The default configuration is in appsettings.json.
//     In Development environment, use Development configuration if available.
//     Otherwise, use Production configuration if available.
IConfiguration appConfig;
if (Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development")
{
    appConfig = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile("appsettings.Development.json", true)
        .Build();
}
else
{
    appConfig = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile("appsettings.Production.json", true)
        .Build();
}

// Get the Discord authentication token for the bot.
string botToken = appConfig.GetValue<string>("DiscordConnection:Token") ?? string.Empty;

// Get the Discord guilds in which the bot will register commands.
IConfigurationSection guildSection = appConfig.GetSection("DiscordConnection:Guilds");
string[] guildIDs = guildSection.Get<string[]>() ?? Array.Empty<string>();
Snowflake?[] guilds = new Snowflake?[guildIDs.Length];
for (int i = 0; i < guildIDs.Length; i++)
{
    if (!DiscordSnowflake.TryParse(guildIDs[i], out guilds[i]))
    {
        // TODO: Handle invalid guild ID.
        return;
    }
}

// Verify that a bot token was set.
if (botToken == string.Empty)
{
    // TODO: Handle missing bot token.
    return;
}

// Verify that the guilds were set.
foreach (Snowflake? guild in guilds)
{
    if (guild?.Value == 0)
    {
        // TODO: Handle missing guild ID.
        return;
    }
}

// Build the host.
IHost host = Host.CreateDefaultBuilder()
    .UseWindowsService()
    .AddDiscordService(_ => botToken)
    .ConfigureServices(services =>
    {
        services
            .AddDiscordCommands(true)
            .AddCommandTree()
                .WithCommandGroup<SimpleCommands>();
    })
    .Build();

// Update slash commands.
SlashService slashService = host.Services.GetRequiredService<SlashService>();
foreach (Snowflake? guild in guilds)
{
    Result updateSlash = await slashService.UpdateSlashCommandsAsync(guild);
    if (!updateSlash.IsSuccess)
    {
        // TODO: Handle failure to update slash commands.
        return;
    }
}

// Run the application.
await host.RunAsync();
