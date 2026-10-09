namespace LocalCommerce.Application.Errors;

/// <summary>
/// Stable, transport-neutral error codes. These values are part of the application
/// contract; callers must not infer them from exception messages.
/// </summary>
public static class ApplicationErrorCodes
{
    public const string RequestInvalid = "request.invalid";
    public const string RequestPayloadTooLarge = "request.payload_too_large";
    public const string AuthenticationRequired = "authentication.required";
    public const string AuthenticationInvalid = "authentication.invalid";
    public const string AuthorizationForbidden = "authorization.forbidden";
    public const string ResourceNotFound = "resource.not_found";
    public const string OrderInvalidState = "order.invalid_state";
    public const string DeliveryInvalidState = "delivery.invalid_state";
    public const string CartNotCheckoutable = "cart.not_checkoutable";
    public const string CatalogItemUnavailable = "catalog.item_unavailable";
    public const string IdempotencyKeyRequired = "idempotency.key_required";
    public const string IdempotencyKeyReused = "idempotency.key_reused";
    public const string IdempotencyResultUnavailable = "idempotency.result_unavailable";
    public const string RateLimitExceeded = "rate_limit.exceeded";
    public const string DependencyUnavailable = "dependency.unavailable";
    public const string InternalUnexpected = "internal.unexpected";
}

/// <summary>
/// An expected, classified application failure. The stable Code is the machine
/// contract; Message is for diagnostics and must never be parsed by adapters.
/// </summary>
public class ApplicationFailureException : Exception
{
    public ApplicationFailureException(string code, string message)
        : base(message)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("An application error code is required.", nameof(code));

        Code = code;
    }

    public ApplicationFailureException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("An application error code is required.", nameof(code));

        Code = code;
    }

    public string Code { get; }
}
