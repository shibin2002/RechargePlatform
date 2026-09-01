namespace RechargePlatform.Common.Constants;

public static class TransactionStatuses
{
    public const string New = "NEW";
    public const string Processing = "PROCESSING";
    public const string Success = "SUCCESS";
    public const string Failed = "FAILED";
    public const string Pending = "PENDING";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        New, Processing, Success, Failed, Pending
    };
}

public static class CardStatuses
{
    public const string Available = "AVAILABLE";
    public const string Reserved = "RESERVED";
    public const string Used = "USED";
    public const string Expired = "EXPIRED";
    public const string Blocked = "BLOCKED";
}

public static class SupportedOperators
{
    public const string Jio = "Jio";
    public const string Airtel = "Airtel";
    public const string Vi = "Vi";
    public const string Bsnl = "BSNL";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Jio, Airtel, Vi, Bsnl
    };

    public static bool IsValid(string? op) => !string.IsNullOrWhiteSpace(op) && All.Contains(op);
}

public static class AuthConstants
{
    public const string ApiKeyHeaderName = "X-Api-Key";
    public const string ConfigApiKeyPath = "Auth:ApiKey";
}
