using Discord;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Tarscord.Core.Features.Personality;
using Tarscord.Core.Services;
using Xunit;

namespace Tarscord.Core.Tests.Features.Personality;

public class GenerateTests
{
    private const string Fallback = "I dare you to write that message, bob.";
    private const ulong Alice = 111111111111111111;

    [Fact]
    public async Task Handle_WhenTheModelAnswers_ReturnsWhatItSaid()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Go on then."))));

        var handler = NewHandler(chatClient);

        // Act
        var response = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        response.Message.Should().Be("Go on then.");
        response.FromModel.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenOllamaIsNotRunning_ReturnsTheCannedLine()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var handler = NewHandler(chatClient);

        // Act
        var response = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        response.Message.Should().Be(Fallback);
        response.FromModel.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenTheModelTimesOut_ReturnsTheCannedLine()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Timed out"));

        var handler = NewHandler(chatClient);

        // Act
        var response = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        response.Message.Should().Be(Fallback);
    }

    [Fact]
    public async Task Handle_WhenTheModelAnswersWithNothing_ReturnsTheCannedLine()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "   "))));

        var handler = NewHandler(chatClient);

        // Act
        var response = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        response.Message.Should().Be(Fallback);
    }

    [Fact]
    public async Task Handle_ForAnyRequest_SendsThePersonalityAsTheSystemPrompt()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Sure."))));

        var handler = NewHandler(chatClient, sarcasmLevel: "9");

        // Act
        await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        await chatClient.Received(1).GetResponseAsync(
            Arg.Is<IEnumerable<ChatMessage>>(messages =>
                messages.Any(message => message.Role == ChatRole.System && message.Text.Contains("9"))),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTheCallerCancels_DoesNotSwallowTheCancellation()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var handler = NewHandler(chatClient);

        // Act
        Func<Task> act = () => handler.HandleAsync(NewCommand(), cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("localhost:11434")]
    public async Task Handle_WithNoOllamaConfigured_ReturnsTheCannedLineWithoutAsking(string? ollamaUrl)
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        var handler = NewHandler(chatClient, ollamaUrl: ollamaUrl);

        // Act
        var response = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        response.Message.Should().Be(Fallback);
        await chatClient.DidNotReceiveWithAnyArgs().GetResponseAsync(default!, default, default);
    }

    [Fact]
    public async Task Handle_WithNoOllamaConfigured_LeavesTheCooldownUnspent()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());
        var handler = NewHandler(Substitute.For<IChatClient>(), ollamaUrl: null, cooldown: cooldown);

        // Act
        await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        cooldown.IsCoolingDown(Alice).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_OnCooldown_ReturnsTheCannedLineWithoutAsking()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());
        cooldown.TryGenerate(Alice);

        var chatClient = Substitute.For<IChatClient>();
        var handler = NewHandler(chatClient, cooldown: cooldown);

        // Act
        var response = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        response.Message.Should().Be(Fallback);
        await chatClient.DidNotReceiveWithAnyArgs().GetResponseAsync(default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenTheModelFails_StillSpendsTheCooldown()
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var cooldown = new GenerationCooldown(new FakeTimeProvider());
        var handler = NewHandler(chatClient, cooldown: cooldown);

        // Act
        await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        cooldown.IsCoolingDown(Alice).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithNoOllamaModel_ReturnsTheCannedLineWithoutAsking(string? ollamaModel)
    {
        // Arrange
        var chatClient = Substitute.For<IChatClient>();
        var handler = NewHandler(chatClient, ollamaModel: ollamaModel);

        // Act
        var response = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        response.Message.Should().Be(Fallback);
        await chatClient.DidNotReceiveWithAnyArgs().GetResponseAsync(default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenTheModelAnswers_SpendsTheCooldown()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());
        var handler = NewHandler(AnsweringChatClient(), cooldown: cooldown);

        // Act
        await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert
        cooldown.IsCoolingDown(Alice).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTheModelIsAsked_ShowsTyping()
    {
        // Arrange
        var channel = Substitute.For<IMessageChannel>();
        var handler = NewHandler(AnsweringChatClient());

        // Act
        await handler.HandleAsync(NewCommand(channel), CancellationToken.None);

        // Assert
        channel.Received(1).EnterTypingState(Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Handle_WithNoOllamaConfigured_DoesNotShowTyping()
    {
        // Arrange
        var channel = Substitute.For<IMessageChannel>();
        var handler = NewHandler(Substitute.For<IChatClient>(), ollamaUrl: null);

        // Act
        await handler.HandleAsync(NewCommand(channel), CancellationToken.None);

        // Assert
        channel.DidNotReceiveWithAnyArgs().EnterTypingState(default);
    }

    [Fact]
    public async Task Handle_OnCooldown_DoesNotShowTyping()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());
        cooldown.TryGenerate(Alice);

        var channel = Substitute.For<IMessageChannel>();
        var handler = NewHandler(AnsweringChatClient(), cooldown: cooldown);

        // Act
        await handler.HandleAsync(NewCommand(channel), CancellationToken.None);

        // Assert
        channel.DidNotReceiveWithAnyArgs().EnterTypingState(default);
    }

    private static IChatClient AnsweringChatClient()
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Go on then."))));

        return chatClient;
    }

    private static Generate.Command NewCommand(IMessageChannel? channel = null) =>
        new("Dare bob to say it out loud.", Fallback, Alice, channel ?? Substitute.For<IMessageChannel>(), "alice");

    private static Generate.Handler NewHandler(
        IChatClient chatClient,
        string sarcasmLevel = "5",
        string? ollamaUrl = "http://localhost:11434",
        GenerationCooldown? cooldown = null,
        string? ollamaModel = "llama3.1")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["sarcasm-level"] = sarcasmLevel,
                ["humor-level"] = "5",
                ["ollama:url"] = ollamaUrl,
                ["ollama:model"] = ollamaModel
            })
            .Build();

        return new Generate.Handler(
            NullLogger<Generate.Handler>.Instance, chatClient, configuration,
            new BotPersonality(configuration), cooldown ?? new GenerationCooldown(new FakeTimeProvider()));
    }
}
