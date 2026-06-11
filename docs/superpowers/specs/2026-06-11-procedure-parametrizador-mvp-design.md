# Parametrizador Trámites — MVP

**Date:** 2026-06-11  
**Status:** Approved  
**ADO Feature:** [#9555](https://dev.azure.com/FlitDevOps/9032fb34-d178-4c62-b1f5-6805b56524b1/_workitems/edit/9555)  
**ADO User Stories:** [#10002](https://dev.azure.com/FlitDevOps/_workitems/edit/10002)–[#10005](https://dev.azure.com/FlitDevOps/_workitems/edit/10005)  
**Runtime consumer:** [#9733](https://dev.azure.com/FlitDevOps/_workitems/edit/9733)  
**Depends on:** Identity (#9548), Admin OT (#9558)

## Summary

SuperAdmin-only wizard to define procedure types (vehicle query mode, semantic actors, static/dynamic documents). Persists definitions and exposes `IProcedureDefinitionService` for runtime #9733.

## Locked decisions

| Area | Decision |
|------|----------|
| Wizard steps | 4: Name → Vehicle → Actors (min 1) → Documents |
| Name | Free text, globally unique |
| Actors | Semantic role only; person type at runtime (#9733) |
| Documents | Label + Static/Dynamic |
| Lifecycle | Create, list, edit, activate/deactivate (no delete) |
| Module | `Flit.Procedures.*` in core-api monolith |
| OT sync | Upsert `procedure_type_catalog` on active persist |
| MVP scope | Wizard + persistence + read contract; low-code deferred |

## User stories

| HU | ADO | Branch |
|----|-----|--------|
| HU-1 | #10002 | `feature/HU10002-DCHICA-schema-procedure-definition-service` |
| HU-2 | #10003 | `feature/HU10003-DCHICA-api-admin-crud` |
| HU-3 | #10004 | `feature/HU10004-DCHICA-fe-index` |
| HU-4 | #10005 | `feature/HU10005-DCHICA-fe-wizard` |

See brainstorming session for full architecture, data model, API, and frontend design.
