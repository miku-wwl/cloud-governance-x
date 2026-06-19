[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern("^[a-z][a-z0-9_]{2,62}$")]
    [string]$Database
)

$ErrorActionPreference = "Stop"
$sql = @"
INSERT INTO organizations
    (id, display_name, status, created_at, updated_at)
VALUES
    ('10000000-0000-0000-0000-000000000001', 'E2E Organization', 'Active', '2026-06-19T00:00:00Z', '2026-06-19T00:00:00Z')
ON CONFLICT (id) DO NOTHING;

INSERT INTO tenants
    (id, organization_id, slug, display_name, status, created_at, updated_at)
VALUES
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'e2e-tenant', 'E2E Tenant', 'Active', '2026-06-19T00:00:00Z', '2026-06-19T00:00:00Z')
ON CONFLICT (id) DO NOTHING;

INSERT INTO provider_connections
    (id, tenant_id, provider, display_name, credential_reference, status, created_at, updated_at)
VALUES
    ('30000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'azure', 'E2E Azure CLI', 'development://azure-cli', 'Active', '2026-06-19T00:00:00Z', '2026-06-19T00:00:00Z')
ON CONFLICT (id) DO NOTHING;

INSERT INTO memberships
    (id, tenant_id, issuer, subject, subject_type, display_name, status, created_at, updated_at)
VALUES
    ('50000000-0000-0000-0000-000000000001',
     '20000000-0000-0000-0000-000000000001',
     'https://e2e.finops.local',
     'e2e-operator',
     'Service',
     'E2E Operator',
     'Active',
     '2026-06-19T00:00:00Z',
     '2026-06-19T00:00:00Z')
ON CONFLICT (id) DO NOTHING;
"@

& docker compose exec -T postgres psql `
    -v ON_ERROR_STOP=1 `
    -U finops `
    -d $Database `
    -c $sql
if ($LASTEXITCODE -ne 0) {
    throw "The explicit E2E Tenant could not be initialized."
}

$accountIds = @("sample-subscription")
$azureAccountIds = & az account list --query "[].id" --output tsv 2>$null
if ($LASTEXITCODE -eq 0) {
    $accountIds += @($azureAccountIds)
}

foreach ($accountId in @(
    $accountIds |
        Where-Object { $_ } |
        Select-Object -Unique
)) {
    if ($accountId -notmatch "^[A-Za-z0-9-]{3,128}$") {
        throw "Azure account ID '$accountId' is not safe for the E2E fixture."
    }

    $accountSql = @"
INSERT INTO cloud_accounts
    (id, tenant_id, provider_connection_id, provider, external_account_id, display_name, status, created_at, updated_at)
SELECT
    gen_random_uuid(),
    '20000000-0000-0000-0000-000000000001',
    '30000000-0000-0000-0000-000000000001',
    'azure',
    '$accountId',
    'E2E Azure account $accountId',
    'Active',
    '2026-06-19T00:00:00Z',
    '2026-06-19T00:00:00Z'
WHERE NOT EXISTS (
    SELECT 1
    FROM cloud_accounts
    WHERE provider = 'azure'
      AND external_account_id = '$accountId'
);
"@

    & docker compose exec -T postgres psql `
        -v ON_ERROR_STOP=1 `
        -U finops `
        -d $Database `
        -c $accountSql
    if ($LASTEXITCODE -ne 0) {
        throw "Azure account '$accountId' could not be initialized."
    }
}
