# Branching and Pull Request Workflow

## Purpose
This repository uses a **plan-based delivery workflow**. The **approved plan** is the unit of branch ownership, execution, and pull request submission.

## Core Rules
1. **One approved plan = one working branch by default.**
2. **One successfully completed plan = one pull request by default.**
3. The **orchestrator** owns branch creation, checkpoint-commit decisions, and PR submission.
4. The **planner** does not create branches, commit changes, or submit PRs.
5. The **coder** may create checkpoint commits **only when explicitly instructed by the orchestrator**.
6. The **designer** does not create branches, commit changes, or submit PRs.
7. Partial-plan PRs are not allowed unless the planner explicitly decomposes the request into **independently shippable plans**.
8. Parallel work may use **git worktrees** only when the orchestrator determines that phases can run concurrently with no file overlap.

## Branch Lifecycle

### When a new branch is created
After:
- the planner has returned a complete plan
- the planner's **Open Questions** section is `None`
- the orchestrator has accepted the plan and is ready to begin implementation

### Who creates the branch
The **orchestrator** creates the working branch.

### Branch naming
Use one of these patterns:
- `feature/<topic>`
- `fix/<topic>`
- `refactor/<topic>`

If a ticket or issue identifier exists, include it:
- `feature/<ticket>-<topic>`
- `fix/<ticket>-<topic>`
- `refactor/<ticket>-<topic>`

Use lowercase letters, numbers, and hyphens only.

## Commit Policy

### Default
Workers do **not** commit automatically after every task.

### Checkpoint commits
A checkpoint commit may be created only when:
- a phase is complete, or
- a meaningful, self-contained milestone is complete, or
- the orchestrator wants a recovery point before a higher-risk next phase

### Who commits
- **Default owner:** orchestrator
- **Exception:** coder may commit only when the orchestrator explicitly delegates it
- **Designer never commits**

### Commit message format
Use clear conventional-style messages:
- `feature: add settings panel for notifications`
- `fix: prevent duplicate refresh requests`
- `refactor: simplify theme token mapping`

If helpful, include the plan scope in the body:
- what phase completed
- what validation passed
- any important limitations

## Pull Request Policy

### When a PR is opened
Open a PR only when:
- the approved plan has completed successfully
- required validation has passed
- the orchestrator has verified that outputs are coherent and within scope

### Who opens the PR
The **orchestrator** opens the PR.

### PR content
The PR should include:
- concise summary of what changed
- files or areas affected
- validation performed
- noteworthy design or implementation constraints
- unresolved issues, if any

### Draft PRs
Default to a normal PR only after successful completion.
Use a **draft PR** only if:
- the user explicitly requests it, or
- the planner explicitly split the work into staged, reviewable deliverables

## Parallel Work and Worktrees
Use git worktrees only when all of the following are true:
1. the orchestrator has identified parallelizable phases
2. the tasks have no overlapping files
3. separate Claude sessions are actually being used
4. the added worktree complexity is justified

Do not create one worktree per agent by default.

## Agent Responsibilities Summary

### Orchestrator
- create the working branch
- decide whether worktrees are needed
- delegate implementation phases
- decide when checkpoint commits happen
- verify final coherence
- open the PR

### Planner
- decide whether the request should stay as one plan or be split into multiple independently shippable plans
- flag whether parallel execution may justify worktrees
- never perform git write actions

### Coder
- implement assigned files
- perform validation
- make a checkpoint commit only if explicitly instructed by the orchestrator

### Designer
- implement assigned presentational files
- perform design/accessibility validation
- never perform git write actions

## Decision Rule for Splitting Plans
Default to **one branch and one PR per approved plan**.

Only split into multiple branches/PRs when the planner explicitly determines that the work consists of independently reviewable, independently shippable deliverables.
