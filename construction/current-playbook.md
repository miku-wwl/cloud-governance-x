# Current Construction Playbook

Active milestone: M4 - RBAC, endpoint protection and audit  
Current project position: Phase 2 after Day 26  
Next unit: Day 27

## 1. Operating Rule

Before implementing a new unit:

1. read [outline.md](../outline.md);
2. read [docs/current-state.md](../docs/current-state.md);
3. read the relevant Day capsule in [docs/days](../docs/days/);
4. check the current risk and production gap registers;
5. verify the working tree is clean or identify unrelated user changes;
6. implement only the current unit unless the Owner changes scope.

## 2. Day 27 Scope

Day 27 should implement permission and scope based RBAC.

The expected boundary:

- define permission names and scope shapes;
- map existing trusted TenantContext and authenticated subject into an
  authorization evaluation contract;
- cover tenant, account and platform scope decisions needed by current API and
  Worker paths;
- add allow/deny matrix tests for administrator, operator, analyst, auditor and
  owner-style subjects where those roles are introduced;
- keep endpoint-wide policy application for Day 28 unless needed only as a
  minimal test harness;
- keep append-only audit persistence for Day 29.

## 3. Day 27 Non-Goals

Day 27 should not claim:

- every existing endpoint is protected;
- stable 401/403 response contracts are complete;
- append-only audit storage is complete;
- PostgreSQL RLS is implemented;
- React or browser identity flows exist.

## 4. Verification Expectations

At minimum:

- `dotnet test FinOpsPlatform.slnx --no-restore` or a full restore/build/test if
  dependencies changed;
- focused authorization tests covering allow and deny paths;
- architecture or static-gate updates if new auth abstractions affect layering;
- migration tests only if schema changes are introduced;
- documentation updates to [docs/current-state.md](../docs/current-state.md),
  [docs/days/day-27.md](../docs/days/day-27.md) and the risk/gap registers.

## 5. Gate Rule

Day 27 remains `Validation` until the Owner accepts the RBAC behavior and the
negative authorization matrix. It does not close Phase 2 by itself.
