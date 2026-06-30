namespace FinOps.Domain.Tenancy;

public enum OrganizationStatus
{
    Active,
    Suspended,
    Decommissioning
}

public enum TenantStatus
{
    Active,
    Suspended,
    Decommissioning
}

public enum ProviderConnectionStatus
{
    Pending,
    Active,
    Degraded,
    Revoked
}

public enum CloudAccountStatus
{
    Pending,
    Active,
    Suspended,
    Disconnected
}

public enum MembershipStatus
{
    Invited,
    Active,
    Suspended,
    Revoked
}

public enum MembershipRole
{
    Owner,
    Administrator,
    Operator,
    Analyst,
    Auditor
}

public enum SubjectType
{
    Human,
    Service
}
