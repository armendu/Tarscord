namespace Tarscord.Core.Features.Common;

internal interface IPerformedByUser
{
    /// <summary>
    /// Represents the unique username of the Discord user.
    /// </summary>
    public string PerformedByUser { get; }
}
