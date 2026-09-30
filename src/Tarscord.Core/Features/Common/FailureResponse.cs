namespace Tarscord.Core.Features.Common;

/// <summary>A failure the user caused and can read, as opposed to a bug.</summary>
public sealed record FailureResponse(string ErrorMessage);
