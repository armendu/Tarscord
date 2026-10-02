namespace Tarscord.Core.Persistence;

/// <summary>Column widths, so a validator cannot disagree with its column.</summary>
internal static class TextLengths
{
    /// <summary>What an embed title shows.</summary>
    public const int Name = 256;

    /// <summary>What event_organizer holds; a Discord username is far shorter.</summary>
    public const int Organizer = 200;

    /// <summary>What Discord lets someone type.</summary>
    public const int FreeText = 2000;
}
