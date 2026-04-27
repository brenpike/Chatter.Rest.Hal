# Branching and Pull Request Workflow

## Purpose
This repository follows **trunk-based development**. The canonical trunk is `main`.

This workflow is **mandatory** for all agent activity unless the user explicitly overrides it for a specific task. It is not optional guidance.

The **approved plan** is the unit of:
- branch ownership
- implementation execution
- checkpoint-commit decisions
- PR submission

## Non-Optional Rules

1. **Never commit directly to `main`.**
2. **Never push directly to `main`.**
3. All changes must be developed on a non-`main` working branch.
4. **One approved plan = one working branch by default.**
5. **One successfully completed plan = one pull request by default.**
6. PRs target `main` unless the user explicitly instructs otherwise.
7. Merges to `main` use **squash merge**.
8. Direct pushes to other non-protected working branches are allowed when they are part of the approved workflow, but checkpoint-commit policy still applies.

## Trunk-Based Development Standard

### Trunk
- The trunk branch is `main`.
- `main` must remain deployable and stable.
- Work is integrated through short-lived branches and pull requests.
- Long-lived feature branches are discouraged.
- Release branches are not part of the default workflow.

### Protected Branches
- `main` is protected.
- Branch protection rules in GitHub reinforce this workflow, but agents must follow this workflow even if technical protections are absent or misconfigured.

## Branch Taxonomy

Use exactly one of these prefixes:

- `feature/<topic>` for new user-facing or internal features
- `bugfix/<topic>` for defect fixes that are not emergency production hotfixes
- `hotfix/<topic>` for urgent production fixes
- `refactor/<topic>` for structural code improvement without intended behavior change
- `chore/<topic>` for maintenance tasks
- `docs/<topic>` for documentation-only work
- `test/<topic>` for test-only work
- `ci/<topic>` for CI/CD or workflow changes

## Branch Naming Rules

### Format
Use one of these formats:

- `<prefix>/<topic>`
- `<prefix>/<ticket>-<topic>`

Examples:
- `feature/add-hal-link-builder`
- `bugfix/123-fix-null-link-serialization`
- `hotfix/456-restore-package-publish`
- `ci/update-dotnet-workflow`

### Naming constraints
- lowercase only
- numbers allowed
- words separated by hyphens
- no spaces
- no underscores
- no extra slashes beyond the prefix separator

### Ticket IDs
- If a ticket or issue identifier exists, include it in the branch name.
- If no ticket exists, omit it.

## Plan-to-Branch Mapping

### Default
One approved plan maps to one working branch.

### Exceptions
Only allow multiple branches or PRs for one user request when the planner explicitly decomposes the work into **independently reviewable and independently shippable plans**.

If the planner does not explicitly split the work, assume exactly one branch and one PR.

## Branch Creation

### When a branch is created
Create the working branch only after:
- the planner has returned a complete plan
- the planner's **Open Questions** section is `None`
- the orchestrator has accepted the plan
- implementation is ready to begin

### Branch authority and required decisions
The orchestrator creates or explicitly confirms the working branch.

Before implementation begins, all of the following must be explicitly defined:
- work classification (`feature`, `bugfix`, `hotfix`, `refactor`, `chore`, `docs`, `test`, `ci`)
- base branch
- working branch name
- whether worktrees are used
- whether checkpoint commits are expected during execution
- intended PR target branch

If any item is undefined, implementation must not begin.

## Commit Policy

### Default
Workers do **not** commit automatically after every task.

### Checkpoint commits
Checkpoint commits are allowed only when:
- a phase is complete
- a meaningful self-contained milestone is complete
- the orchestrator wants a recovery point before a higher-risk next phase

### Who commits
- **Default owner:** orchestrator
- **Exception:** coder may commit only when explicitly instructed by orchestrator
- **Designer never commits**

### Commit message convention
Use conventional-style commit messages.

Allowed types:
- `feat`
- `fix`
- `hotfix`
- `refactor`
- `docs`
- `test`
- `chore`
- `ci`

Examples:
- `feat: add embedded resource builder overload`
- `fix: prevent null link serialization failure`
- `hotfix: restore package publish path`
- `ci: update workflow trigger filters`

### Commit hygiene
- Do not mix unrelated changes in one commit.
- Stage only the files that belong to the completed phase or approved milestone.
- Do not create checkpoint commits on `main`.

## Version Bump Policy

A version bump is **required** when a PR changes non-markdown files under a packable package's `src/` directory. See [docs/versioning.md](versioning.md) for the full policy.

### When a bump is required

| Changed paths | Bump required for |
|---|---|
| `src/Chatter.Rest.Hal/**` (non-`.md`) | `Chatter.Rest.Hal` |
| `src/Chatter.Rest.Hal.CodeGenerators/**` (non-`.md`) | `Chatter.Rest.Hal.CodeGenerators` |
| `src/Chatter.Rest.Hal.Core/**` (non-`.md`) | Whichever dependent packages are affected |

No bump required for: `docs/**`, `test/**`, `.github/workflows/**`, governance files, `*.md`.

### Bump type

The orchestrator determines the bump type (major/minor/patch) by examining commit types and public API impact. When ambiguous, the orchestrator asks the user to confirm before delegating.

| Commit type | API impact | SemVer increment |
|---|---|---|
| `feat` | New API | minor |
| `feat!` / `BREAKING CHANGE:` | Breaking | major |
| `fix`, `bugfix`, `refactor` | None | patch |
| `chore`, `docs`, `test`, `ci` | None | **no bump** |

### Execution

The orchestrator delegates version file edits to the coder agent. The bump is included in the **same PR** as the feature or fix — not a follow-up PR.

Files updated atomically per bump (see [docs/versioning.md](versioning.md) for full list):
- `.csproj` `<Version>` element (source of truth)
- `CLAUDE.md` Package Versions table
- `docs/architecture.md` solution structure table
- `CHANGELOG.md` or `CHANGELOG-CodeGenerators.md`

### PR readiness gate

A PR that changes non-markdown `src/` files is **not ready to merge** until the version bump is included. The CI `version-check` job enforces this by comparing the `.csproj` version against the latest git tag and failing if they are equal.

## Pull Request Policy

### When a PR is opened
Open a PR only when:
- the approved plan is complete
- required validation has passed
- outputs are coherent and within scope
- the branch is ready to merge into `main`

### Who opens the PR
The **orchestrator** opens the PR.

### Branch-to-PR mapping
- One working branch produces one PR.
- After merge or closure, the branch is considered complete.
- Follow-up work should use a new branch unless the PR is merely paused and immediately resumed.

### PR target
- Default target: `main`
- Any different target branch requires explicit user direction

### PR type
Default to a normal PR.

Use a draft PR only when:
- the user explicitly requests it, or
- the planner explicitly split the work into staged reviewable deliverables

### PR content
The PR must include:
- concise summary of what changed
- key files or areas affected
- validation performed
- notable implementation or design constraints
- unresolved issues, if any
  
Never include "co-authored by..." or "Generated by..." or similar text.

## Merge Policy

### Required path to trunk
Changes reach `main` only through a pull request.

### Merge method
Use **squash merge** for PRs merged into `main`.

### Review requirement
At least one human review is required before merge to `main`.

### CI requirement
Required validation must pass before the PR is considered ready to merge.

## Validation Gate

Before PR creation, the orchestrator must confirm that all relevant required checks completed successfully.

Minimum expectation:
- relevant build checks passed
- relevant tests passed

Do not invent missing validation.
Do not open a PR if validation is incomplete.

## Syncing With Trunk

When a working branch falls behind `main`:
- prefer rebasing the working branch onto `main` when practical
- avoid unnecessary merge commits from `main` into the working branch
- if conflict resolution materially changes scope or risk, stop and reassess before continuing

## Hotfix Standard

For urgent production fixes:
- create a `hotfix/<topic>` branch from `main`
- implement the minimal safe change
- validate the hotfix
- open a PR back to `main`
- merge via squash after required review/approval path unless the user explicitly directs a different emergency process

## Parallel Work and Worktrees

Worktrees are optional.

Use git worktrees only when all are true:
1. the orchestrator has identified parallelizable phases
2. file scopes do not overlap
3. separate Claude sessions are actually being used
4. the added complexity is justified

Do not create one worktree per agent by default.
Do not use worktrees when a simple sequential flow is safer.

## Branch Cleanup

After a PR is merged:
- delete the working branch

After a PR is closed unmerged:
- create a new branch for follow-up work unless the same PR is being resumed immediately

## Scope Drift and Replanning

If implementation reveals extra work not covered by the approved plan:
1. stop
2. re-evaluate scope
3. replan if needed

Remain on the same branch only if the added work is still within the same approved deliverable.
If the new work is materially distinct, create a new approved plan and a new branch.

## Enforcement Rule

This workflow is mandatory.

If the orchestrator has not explicitly established:
- branch classification
- base branch
- working branch
- worktree decision
- commit policy
- PR path

then implementation must not begin.

Failure to follow this workflow is a non-compliant execution state and must be treated as a blocker rather than ignored.