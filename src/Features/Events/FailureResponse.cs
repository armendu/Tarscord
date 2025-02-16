namespace Tarscord.Core.Features.Events;

internal record FailureResponse(string ErrorMessage, string? ErrorDescription = null);