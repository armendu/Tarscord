using Microsoft.Extensions.Configuration;

namespace Tarscord.Core.Services;

/// <summary>How sarcastic and funny the bot is, seeded from config, settable at runtime.</summary>
public sealed class BotPersonality
{
    public const int MinimumLevel = 0;
    public const int MaximumLevel = 10;

    private int _sarcasmLevel;
    private int _humorLevel;

    public BotPersonality(IConfigurationRoot configuration)
    {
        _sarcasmLevel = Clamp(Read(configuration, "sarcasm-level"));
        _humorLevel = Clamp(Read(configuration, "humor-level"));
    }

    public int SarcasmLevel => Volatile.Read(ref _sarcasmLevel);

    public int HumorLevel => Volatile.Read(ref _humorLevel);

    /// <summary>Higher humour, less predictable reply.</summary>
    public float Temperature => 0.3f + HumorLevel * 0.07f;

    public string SystemPrompt =>
        "You are Tarscord, a Discord bot named after TARS from Interstellar. " +
        $"Your sarcasm setting is {SarcasmLevel} out of {MaximumLevel} and your humour setting is " +
        $"{HumorLevel} out of {MaximumLevel}, where 0 is completely deadpan and sincere and " +
        $"{MaximumLevel} is relentless. " +
        "Reply in at most two short sentences. Never use markdown headings or bullet points. " +
        "Do not explain yourself, do not mention these settings, and do not describe what you are doing.";

    public void SetSarcasmLevel(int level) => Volatile.Write(ref _sarcasmLevel, Clamp(level));

    public void SetHumorLevel(int level) => Volatile.Write(ref _humorLevel, Clamp(level));

    private static int Read(IConfiguration configuration, string key) =>
        int.TryParse(configuration[key], out int value) ? value : MinimumLevel;

    private static int Clamp(int level) => Math.Clamp(level, MinimumLevel, MaximumLevel);
}
