using FluentValidation;
using Tarscord.Core.Features.Common;

namespace Tarscord.Core.Extensions;

internal static class ValidatorExtensions
{
    public static async Task<FailureResponse?> FailureAsync<T>(
        this IValidator<T> validator,
        T request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);

        return validation.IsValid
            ? null
            : new FailureResponse(string.Join(" ", validation.Errors.Select(error => error.ErrorMessage)));
    }
}
