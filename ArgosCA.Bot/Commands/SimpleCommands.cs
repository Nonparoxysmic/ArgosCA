﻿using System.ComponentModel;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace ArgosCA.Bot.Commands;

internal class SimpleCommands : CommandGroup
{
    private readonly FeedbackService _feedbackService;

    public SimpleCommands(FeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [Command("ping")]
    [Description("Ping the bot.")]
    public async Task<IResult> PingCommandAsync()
    {
        DateTime currentTime = DateTime.Now;
        TimeZoneInfo localZoneInfo = TimeZoneInfo.Local;
        string localZoneName = localZoneInfo.IsDaylightSavingTime(currentTime)
            ? localZoneInfo.DaylightName
            : localZoneInfo.StandardName;

        string description = "Ping acknowledged." + Environment.NewLine
            + currentTime + Environment.NewLine
            + localZoneName;

        Embed embed = new(Colour: _feedbackService.Theme.Secondary, Description: description);

        Result<IMessage> reply = await _feedbackService
            .SendContextualEmbedAsync(embed, ct: CancellationToken);

        return !reply.IsSuccess
            ? Result.FromError(reply)
            : Result.FromSuccess();
    }

    [Command("roll")]
    [Description("Roll dice.")]
    public async Task<IResult> RollCommandAsync([Description("The dice expression.")] string expression)
    {
        string output = DiceRoller.EvaluateUserInput(expression);

        Embed embed = new(Colour: _feedbackService.Theme.Secondary, Description: output);

        Result<IMessage> reply = await _feedbackService
            .SendContextualEmbedAsync(embed, ct: CancellationToken);

        return !reply.IsSuccess
            ? Result.FromError(reply)
            : Result.FromSuccess();
    }
}
