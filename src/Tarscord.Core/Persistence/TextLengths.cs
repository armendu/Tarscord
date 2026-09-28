namespace Tarscord.Core.Persistence;

/// <summary>
/// The column widths the validators enforce, kept in one place because a validator that disagrees
/// with its column turns a readable reply into an unhandled Postgres error.
/// </summary>
internal static class TextLengths
{
    /// <summary>What an embed title can show.</summary>
    public const int Name = 256;

    /// <summary>What Discord lets someone type in one message.</summary>
    public const int FreeText = 2000;
}
