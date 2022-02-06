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

    [Command("test")]
    public async Task<IResult> TestCommandAsync()
    {
        Embed embed = new(Colour: _feedbackService.Theme.Secondary, Description: "/test");

        Result<IMessage> reply = await _feedbackService
            .SendContextualEmbedAsync(embed, ct: this.CancellationToken);

        return !reply.IsSuccess
            ? Result.FromError(reply)
            : Result.FromSuccess();
    }
}
