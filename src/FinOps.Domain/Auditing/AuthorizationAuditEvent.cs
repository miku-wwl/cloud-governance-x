namespace FinOps.Domain.Auditing;

public sealed class AuthorizationAuditEvent
{
    private AuthorizationAuditEvent()
    {
    }

    private AuthorizationAuditEvent(
        DateTimeOffset occurredAt,
        string permission,
        string scopeKind,
        Guid? tenantId,
        Guid? cloudAccountId,
        bool isAllowed,
        bool isHighPrivilege,
        string reason,
        string? actorIssuer,
        string? actorSubject,
        string httpMethod,
        string path,
        int statusCode,
        string correlationId)
    {
        Id = Guid.NewGuid();
        OccurredAt = occurredAt;
        Permission = NormalizeRequired(permission, 64, nameof(permission));
        ScopeKind = NormalizeRequired(scopeKind, 32, nameof(scopeKind));
        TenantId = tenantId;
        CloudAccountId = cloudAccountId;
        IsAllowed = isAllowed;
        IsHighPrivilege = isHighPrivilege;
        Reason = NormalizeRequired(reason, 512, nameof(reason));
        ActorIssuer = NormalizeOptional(actorIssuer, 512);
        ActorSubject = NormalizeOptional(actorSubject, 256);
        HttpMethod = NormalizeRequired(httpMethod, 16, nameof(httpMethod));
        Path = NormalizeRequired(path, 1024, nameof(path));
        StatusCode = statusCode;
        CorrelationId = NormalizeRequired(correlationId, 128, nameof(correlationId));
    }

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string Permission { get; private set; } = string.Empty;

    public string ScopeKind { get; private set; } = string.Empty;

    public Guid? TenantId { get; private set; }

    public Guid? CloudAccountId { get; private set; }

    public bool IsAllowed { get; private set; }

    public bool IsHighPrivilege { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public string? ActorIssuer { get; private set; }

    public string? ActorSubject { get; private set; }

    public string HttpMethod { get; private set; } = string.Empty;

    public string Path { get; private set; } = string.Empty;

    public int StatusCode { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public static AuthorizationAuditEvent Record(
        DateTimeOffset occurredAt,
        string permission,
        string scopeKind,
        Guid? tenantId,
        Guid? cloudAccountId,
        bool isAllowed,
        bool isHighPrivilege,
        string reason,
        string? actorIssuer,
        string? actorSubject,
        string httpMethod,
        string path,
        int statusCode,
        string correlationId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(statusCode, 100);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(statusCode, 599);

        return new AuthorizationAuditEvent(
            occurredAt,
            permission,
            scopeKind,
            tenantId,
            cloudAccountId,
            isAllowed,
            isHighPrivilege,
            reason,
            actorIssuer,
            actorSubject,
            httpMethod,
            path,
            statusCode,
            correlationId);
    }

    private static string NormalizeRequired(
        string value,
        int maxLength,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
