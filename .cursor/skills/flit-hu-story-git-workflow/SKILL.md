---
name: flit-hu-story-git-workflow
description: Use when implementing Azure DevOps user stories (HUs) sequentially in FLIT EVOLUTION, creating one feature branch per story, or when the user asks to pause for review between stories before continuing.
---

# FLIT HU Story-by-Story Git Workflow

## Overview

One HU per branch, one branch at a time. Finish → summarize → **stop and wait** for human review before starting the next HU.

**Violating the letter of these rules is violating the spirit of these rules.**

## When to use

- Implementing a feature decomposed into ADO user stories (e.g. #9804–#9815)
- User says "story by story", "one HU at a time", or "wait for my review"
- Starting work on a specific HU number

**When NOT to use:** exploratory spikes, docs-only edits, or user explicitly requests multi-story branches.

## Branch naming (mandatory)

```
feature/HU{storyId}-DCHICA-{descriptive-slug}
```

| Part | Rule | Example |
|------|------|---------|
| `storyId` | ADO User Story numeric ID | `9804` |
| `DCHICA` | Fixed author token | literal `DCHICA` |
| `descriptive-slug` | kebab-case, lowercase, ASCII, ≤40 chars | `schema-postgresql-ef-core` |

**Derive slug from story title:** drop `[BACKEND]` / `[FRONTEND]`, drop `– Admin Compañías –`, normalize accents (`compañías` → `companias`), replace spaces/punctuation with `-`.

```
[BACKEND] – Admin Compañías – Schema PostgreSQL, entidades y migraciones EF Core
→ feature/HU9804-DCHICA-schema-postgresql-ef-core
```

## Per-story cycle (strict order)

```
1. READ   — HU in ADO (AC, dependencies, design spec section)
2. BRANCH — from updated main (see commands below)
3. BUILD  — implement only that HU's scope
4. VERIFY — tests + build per flit-gestion-hu
5. STOP   — post summary; do NOT start next HU
6. WAIT   — user corrections/tweaks on same branch if needed
7. NEXT   — only after explicit user go-ahead ("continue", "next story", "start #9805")
```

### Git commands (each new HU)

```bash
# 1. Return to main and sync
git checkout main
git pull origin main

# 2. Create story branch (replace ID and slug)
git checkout -b feature/HU9804-DCHICA-schema-postgresql-ef-core

# 3. After work — commit on story branch (user must ask to commit)
git add <files>
git commit -m "feat(companies): schema PostgreSQL y migraciones EF Core (#9804)"
```

**Prohibited without user approval:**

- Starting HU N+1 while HU N awaits review
- Implementing multiple HUs on one branch
- Force-push, amend after push, or merge to `main`
- Pushing to remote unless user asks

## End-of-story summary (required before waiting)

Post this template after each HU:

```markdown
## HU #{id} — {title}

**Branch:** `feature/HU{id}-DCHICA-{slug}`

### Changes
- {bullet: files/areas touched}

### Behavior
- {what now works per AC}

### Verification
- {commands run + results}

### Notes / open questions
- {anything needing your decision}

---
Waiting for your review before starting the next story.
```

## Related skills

| Skill | When |
|-------|------|
| `flit-gestion-hu` | ADO Active → Resolved, build gate, QA handoff |
| `flit-azure-devops` | MCP/API for work item updates |
| `flit-integration-ado` | Register GitHub PR on ADO after user opens PR |
| Design spec in `docs/superpowers/specs/` | Architecture reference for the feature |

## Rationalization table

| Excuse | Reality |
|--------|---------|
| "Next story is tiny, I'll batch it" | One HU = one branch. Batch only if user explicitly allows. |
| "I'll branch later" | Branch **before** first line of implementation code. |
| "Summary can wait until all stories done" | Summary + stop **after every** HU. |
| "User said continue implicitly" | Need explicit approval for next HU. |
| "Fix belongs on main" | Fixes for current HU stay on current story branch. |
| "I'll push without asking" | Push only when user requests. |

## Red flags — STOP

- Coding on `main` for story work
- Two HUs on one branch
- Moving to next story without user reply after summary
- Branch name missing `HU{id}-DCHICA-`
