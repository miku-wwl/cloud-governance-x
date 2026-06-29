# Day 27 - Permission And Scope RBAC

## Status

Planned next unit.

## Goal

Define and implement permission plus scope based RBAC on top of the Day 20-26
identity and tenant foundation.

## Expected Scope

- permission vocabulary;
- role or grant model needed by current API and Worker paths;
- tenant, CloudAccount and platform scope evaluation;
- allow and deny matrix tests;
- integration with authenticated `iss`/`sub` and trusted TenantContext;
- documentation updates to current state, risks and production gaps.

## Non-Goals

- full endpoint protection for every route, unless only needed as a minimal
  Day 27 harness;
- stable 401/403 Problem Details for the whole API;
- append-only audit persistence;
- PostgreSQL RLS;
- React frontend authorization.

## Required Reading

- [construction/current-playbook.md](../../construction/current-playbook.md)
- [docs/current-state.md](../current-state.md)
- [docs/roadmap.md](../roadmap.md)
- [day-26.md](day-26.md)

## Gate Expectation

Day 27 should stay in `Validation` until negative authorization paths are
reviewed and accepted. It does not close Phase 2 by itself.
