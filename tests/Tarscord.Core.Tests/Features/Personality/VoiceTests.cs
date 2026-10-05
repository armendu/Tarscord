using Discord;
using Discord.Commands;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Personality;
using Tarscord.Core.Services;
using Xunit;

namespace Tarscord.Core.Tests.Features.Personality;

public class VoiceTests
{
    private const string Generated = "Another IOU for the collection.";
    private const string ReplyTitle = "bob owes alice 20.00";
    private const string ReplyDescription = "for lunch";

    [Fact]
    public async Task Handle_WhenTheModelAnswers_PutsItsLineOverTheReply()
    {
        // Arrange
        var handler = NewHandler(AnsweringChatClient());

        // Act
        var embed = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        embed.Title.Should().Be(Generated);
    }

    [Fact]
    public async Task Handle_WhenTheModelAnswers_KeepsTheReplyUnderTheLine()
    {
        // Arrange
        var handler = NewHandler(AnsweringChatClient());

        // Act
        var embed = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        embed.Description.Should().Be($"{ReplyTitle}\n{ReplyDescription}");
    }

    [Fact]
    public async Task Handle_WhenTheModelQuotesItsLine_UsesItUnquoted()
    {
        // Arrange
        var handler = NewHandler(AnsweringChatClient($"\"{Generated}\""));

        // Act
        var embed = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        embed.Title.Should().Be(Generated);
    }

    [Fact]
    public async Task Handle_WhenTheModelFails_ReturnsTheReplyUnchanged()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var handler = NewHandler(chatClient);

        // Act
        var embed = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        (embed.Title, embed.Description).Should().Be((ReplyTitle, ReplyDescription));
    }

    [Fact]
    public async Task Handle_WithNoModelConfigured_ReturnsTheReplyUnchanged()
    {
        // Arrange
        var handler = NewHandler(AnsweringChatClient(), ollamaUrl: "");

        // Act
        var embed = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        (embed.Title, embed.Description).Should().Be((ReplyTitle, ReplyDescription));
    }

    [Fact]
    public async Task Handle_ForAnyReply_NeverTellsTheModelItsDetails()
    {
        // Arrange
        var chatClient = AnsweringChatClient();
        var handler = NewHandler(chatClient);

        // Act
        await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        await chatClient.Received(1).GetResponseAsync(
            Arg.Is<IEnumerable<ChatMessage>>(messages => messages.All(message =>
                !message.Text.Contains(ReplyTitle) && !message.Text.Contains(ReplyDescription))),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    private static IChatClient AnsweringChatClient(string answer = Generated)
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, answer))));

        return chatClient;
    }

    private static Voice.Command NewCommand()
    {
        var user = Substitute.For<IUser>();
        user.Id.Returns(111111111111111111UL);
        user.Username.Returns("alice");

        var context = Substitute.For<ICommandContext>();
        context.User.Returns(user);
        context.Channel.Returns(Substitute.For<IMessageChannel>());

        return new Voice.Command(
            ReplyTitle.EmbedMessage(ReplyDescription),
            "Someone recorded that another person owes them money.",
            "loan to",
            context);
    }

    private static Voice.Handler NewHandler(IChatClient chatClient, string ollamaUrl = "http://localhost:11434")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["sarcasm-level"] = "5",
                ["humor-level"] = "5",
                ["ollama:url"] = ollamaUrl,
                ["ollama:model"] = "llama3.1"
            })
            .Build();

        var generate = new Generate.Handler(
            NullLogger<Generate.Handler>.Instance, chatClient, configuration,
            new BotPersonality(configuration), new GenerationCooldown(new FakeTimeProvider()));

        return new Voice.Handler(NullLogger<Voice.Handler>.Instance, generate);
    }
}
