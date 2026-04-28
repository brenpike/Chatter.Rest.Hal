# Branching and Pull Request Workflow

## Purpose

This repository follows trunk-based development. The canonical trunk is `main`.

This workflow is mandatory for all agent activity unless the user explicitly overrides it for a specific task. It is not optional guidance.

The approved plan is the unit of:
- branch ownership
- implementation execution
- checkpoint-commit decisions
- pull request submission
- external review remediation

## Non-Optional Rules

1. Never commit directly to `main`.
2. Never push directly to `main`.
3. All changes must be developed on a non-`main` working branch.
4. One approved plan = one working branch by default.
5. One successfully completed plan = one pull request by default.
6. PRs target `main` unless the user explicitly instructs otherwise.
7. Merges to `main` use squash merge.
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
- Branch protection rules reinforce this workflow, but agents must follow this workflow even if technical protections are absent or misconfigured.

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
- `feature/add-resource-builder`
- `bugfix/123-fix-null-serialization`
- `hotfix/456-restore-package-publish`
- `ci/update-build-workflow`

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
Only allow multiple branches or PRs for one user request when the planner explicitly decomposes the work into independently reviewable and independently shippable plans.

If the planner does not explicitly split the work, assume exactly one branch and one PR.

## Branch Creation

### When a branch is created
Create the working branch only after:
- the planner has returned a complete plan
- the planner's open questions are `None`
- the orchestrator has accepted the plan
- implementation is ready to begin

### Branch authority and required decisions
The orchestrator creates or explicitly confirms the working branch.

Before implementation begins, all of the following must be explicitly defined:
- work classification
- base branch
- working branch name
- whether worktrees are used
- whether checkpoint commits are expected during execution
- intended PR target branch

If any item is undefined, implementation must not begin.

## Commit Policy

### Default
Workers do not commit automatically after every task.

### Checkpoint commits
Checkpoint commits are allowed only when:
- a phase is complete
- a meaningful self-contained milestone is complete
- the orchestrator wants a recovery point before a higher-risk next phase
- a review-remediation fix is complete, validated, and ready to push

### Who commits
- Default owner: orchestrator
- Exception: coder may commit only when explicitly instructed by orchestrator
- Designer never commits

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
- `feat: add resource builder overload`
- `fix: prevent null serialization failure`
- `hotfix: restore package publish path`
- `ci: update workflow trigger filters`

### Commit hygiene
- Do not mix unrelated changes in one commit.
- Stage only the files that belong to the completed phase, approved milestone, or review-remediation item.
- Do not create checkpoint commits on `main`.

## Version Bump Policy

`versioning.md` is the canonical source for SemVer and version bump rules.

A PR is not ready to merge until any required version/release metadata changes are included.

The orchestrator determines whether a version bump is required and delegates version/release file edits to coder.

Version bump changes must be included in the same PR as the triggering change unless the user explicitly directs otherwise.

## Pull Request Policy

### When a PR is opened
Open a PR only when:
- the approved plan is complete
- required validation has passed
- outputs are coherent and within scope
- required version/release metadata changes are included
- the branch is ready to merge into `main`

### Who opens the PR
The orchestrator opens the PR.

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
- version/release metadata notes when relevant
- notable implementation or design constraints
- unresolved issues, if any

Never include "co-authored by...", "Generated by...", or similar text.

## External Review Policy

After a PR is opened, the orchestrator may request external AI review.

External review remediation stays on the same PR branch unless:
- the feedback is materially outside the approved plan
- the feedback requires a separate independently shippable change
- the PR has already been merged or closed

Review-remediation commits are allowed on the existing PR branch when they directly address PR feedback.

The orchestrator remains responsible for:
- confirming the PR branch is current
- verifying working tree state before remediation
- ensuring remediation commits are scoped to review feedback
- pushing remediation commits to the PR branch
- replying to and resolving review threads according to policy
- requesting re-review only after new commits or a clear written rationale

Do not open a new PR solely to address review comments on an active PR unless the feedback is outside the approved plan.

## Merge Policy

### Required path to trunk
Changes reach `main` only through a pull request.

### Merge method
Use squash merge for PRs merged into `main`.

### Review requirement
At least one human review is required before merge to `main`.

### CI requirement
Required validation must pass before the PR is considered ready to merge.

## Validation Gate

Before PR creation, the orchestrator must confirm that all relevant required checks completed successfully.

Minimum expectation:
- relevant build checks passed
- relevant tests passed
- required version/release metadata checks passed when applicable

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
