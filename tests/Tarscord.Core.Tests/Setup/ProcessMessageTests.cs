using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Tarscord.Core.Features.Personality;
using Tarscord.Core.Services;
using Tarscord.Core.Setup;
using Xunit;

namespace Tarscord.Core.Tests.Setup;

public class ProcessMessageTests
{
    private const ulong Alice = 111111111111111111;
    private const string Said = "what do you think?";
    private const string Generated = "Go on then.";

    [Fact]
    public void IsFromPerson_ForAPerson_IsTrue()
    {
        // Arrange
        var author = NewAuthor(isBot: false, isWebhook: false);

        // Act
        bool fromPerson = ProcessMessage.IsFromPerson(author);

        // Assert
        fromPerson.Should().BeTrue();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void IsFromPerson_ForABotOrWebhook_IsFalse(bool isBot, bool isWebhook)
    {
        // Arrange
        var author = NewAuthor(isBot, isWebhook);

        // Act
        bool fromPerson = ProcessMessage.IsFromPerson(author);

        // Assert
        fromPerson.Should().BeFalse();
    }

    [Fact]
    public async Task AnswerMention_OnCooldown_SendsNothing()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());
        cooldown.TryGenerate(Alice);

        var configuration = NewConfiguration();
        using var discord = new DiscordSocketClient();
        using var commands = new CommandService();
        var handler = NewHandler(discord, commands, configuration, cooldown);
        var channel = Substitute.For<IMessageChannel>();

        // Act
        await handler.AnswerMentionAsync(NewGenerate(configuration, cooldown), channel, NewUser(), Said);

        // Assert
        await channel.DidNotReceiveWithAnyArgs().SendMessageAsync();
    }

    [Fact]
    public async Task AnswerMention_OffCooldown_RepliesWithAnEmbed()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());

        var configuration = NewConfiguration();
        using var discord = new DiscordSocketClient();
        using var commands = new CommandService();
        var handler = NewHandler(discord, commands, configuration, cooldown);
        var channel = Substitute.For<IMessageChannel>();

        // Act
        await handler.AnswerMentionAsync(NewGenerate(configuration, cooldown), channel, NewUser(), Said);

        // Assert
        await channel.Received(1).SendMessageAsync(embed: Arg.Is<Embed>(embed => embed.Description == Generated));
    }

    private static IConfigurationRoot NewConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["sarcasm-level"] = "5",
                ["humor-level"] = "5",
                ["ollama:url"] = "http://localhost:11434",
                ["ollama:model"] = "llama3.1"
            })
            .Build();

    private static ProcessMessage.Handler NewHandler(
        DiscordSocketClient discord,
        CommandService commands,
        IConfigurationRoot configuration,
        GenerationCooldown cooldown) =>
        new(discord, commands, configuration, Substitute.For<IServiceProvider>(), cooldown,
            NullLogger<ProcessMessage.Handler>.Instance);

    private static Generate.Handler NewGenerate(IConfigurationRoot configuration, GenerationCooldown cooldown)
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, Generated))));

        return new Generate.Handler(
            NullLogger<Generate.Handler>.Instance, chatClient, configuration,
            new BotPersonality(configuration), cooldown);
    }

    private static IUser NewUser()
    {
        var user = Substitute.For<IUser>();
        user.Id.Returns(Alice);
        user.Username.Returns("alice");

        return user;
    }

    private static IUser NewAuthor(bool isBot, bool isWebhook)
    {
        var author = Substitute.For<IUser>();
        author.IsBot.Returns(isBot);
        author.IsWebhook.Returns(isWebhook);

        return author;
    }
}
