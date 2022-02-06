using ArgosCA.Bot;
using ArgosCA.Bot.Commands;
using Remora.Commands.Extensions;
using Remora.Discord.API;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Hosting.Extensions;
using Remora.Rest.Core;
using Remora.Results;

// The Discord authentication token for the bot.
string botToken = "";

// The Discord guilds in which the bot will register commands.
Snowflake?[] guilds = Array.Empty<Snowflake?>();

// In Debug configuration, get debug botToken and guild from user secrets.
#if DEBUG
IConfiguration config = new ConfigurationBuilder()
    .AddUserSecrets("6725952A-852B-4D2D-AEF4-FD2649E173A9").Build();
botToken = config.GetValue<string>("DEBUG_BOT_TOKEN");
string guildID = config.GetValue<string>("DEBUG_GUILD_ID");
if (!DiscordSnowflake.TryParse(guildID, out Snowflake? debugGuild))
{
    // TODO: Handle invalid guild ID.
    return;
}
guilds = new Snowflake?[] { debugGuild };
#endif

// In Release configuration, get botToken and guilds from {To Be Determined}.
#if !DEBUG
// TODO: Get botToken and guilds.
#endif

// Verify that a bot token was set.
if (botToken == "")
{
    // TODO: Handle missing bot token.
    return;
}

// Build the host.
IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .AddDiscordService(_ => botToken)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>()
            .AddDiscordCommands(true)
            .AddCommandTree()
                .WithCommandGroup<SimpleCommands>();
    })
    .Build();

// Update slash commands.
SlashService slashService = host.Services.GetRequiredService<SlashService>();
Result checkSlashSupport = slashService.SupportsSlashCommands();
if (!checkSlashSupport.IsSuccess)
{
    // TODO: Handle lack of slash command support.
    return;
}
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
