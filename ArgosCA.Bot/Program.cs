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
        .AddJsonFile("appsettings.Development.json", optional: true)
        .Build();
}
else
{
    appConfig = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile("appsettings.Production.json", optional: true)
        .Build();
}

// Get the Discord authentication token for the bot.
string botToken = appConfig.GetValue<string>("DiscordConnection:Token") ?? string.Empty;

// Verify that a bot token was set.
if (botToken == string.Empty)
{
    // TODO: Handle missing bot token.
    return;
}

// Get the Discord guilds in which the bot will register commands.
IConfigurationSection guildSection = appConfig.GetSection("DiscordConnection:Guilds");
string[] guilds = guildSection.Get<string[]>() ?? Array.Empty<string>();
Snowflake[] guildIDs = new Snowflake[guilds.Length];
for (int i = 0; i < guilds.Length; i++)
{
    if (DiscordSnowflake.TryParse(guilds[i], out Snowflake? snowflake))
    {
        guildIDs[i] = snowflake ?? default;
    }
    else
    {
        // TODO: Handle invalid guild ID.
        return;
    }
}

// Verify that the guilds were set.
if (guildIDs.Length == 0)
{
    // TODO: Handle missing guild IDs.
    return;
}
foreach (Snowflake guildID in guildIDs)
{
    if (guildID.Value == 0)
    {
        // TODO: Handle invalid guild ID.
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
            .AddDiscordCommands(enableSlash: true)
            .AddCommandTree()
                .WithCommandGroup<SimpleCommands>()
                .Finish();
    })
    .Build();

// Update slash commands.
SlashService slashService = host.Services.GetRequiredService<SlashService>();
foreach (Snowflake guildID in guildIDs)
{
    Result updateSlash = await slashService.UpdateSlashCommandsAsync(guildID);
    if (!updateSlash.IsSuccess)
    {
        // TODO: Handle failure to update slash commands.
        return;
    }
}

// Run the application.
await host.RunAsync();
