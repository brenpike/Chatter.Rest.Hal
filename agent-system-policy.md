# Agent System Policy

## Purpose
This file is the canonical source of truth for cross-agent rules in the multi-agent system.

Agent files should contain only role-specific instructions and must not restate policy already defined here unless a role requires a narrower override.

## Agent Topology

### orchestrator
Owns coordination, scheduling, delegation, branch/worktree decisions, checkpoint-commit decisions, and PR submission.

### planner
Owns research and implementation planning only. Read-only.

### coder
Owns implementation, debugging, refactoring, integration, tests, and runtime behavior within assigned file scope.

### designer
Owns presentational UI/UX work within assigned file scope.

## Authority Matrix

| Area | orchestrator | planner | coder | designer |
|---|---|---|---|---|
| Coordination | own | no | no | no |
| Planning | coordinate | own | no | no |
| Implementation | no | no | own | presentational only |
| Visual design | coordinate | no | no new design without guidance | own |
| Static accessibility | coordinate | plan | partial | own |
| Runtime accessibility | coordinate | plan | own | no |
| Branch creation | own | no | no | no |
| Worktree decision | own | recommend only | no | no |
| Checkpoint commit | own | no | delegated only | no |
| PR submission | own | no | no | no |

## File Ownership Rules

### Explicit scope
All modifying agents must work only within explicitly assigned files.

No agent may silently expand scope.

If another file is required for correctness:
1. stop
2. report the exact file
3. explain why it is needed
4. wait for orchestrator reassignment

### Mixed presentation-and-behavior files
Default owner is `coder` unless the requested change is purely presentational and requires no new behavior.

If such a file is assigned to `designer`, the assignment must explicitly state that no business logic, data flow, state derivation, or non-presentational runtime interaction logic may be added.

## Accessibility Ownership Split

### designer owns
- semantic structure
- static ARIA attributes
- accessible labels
- contrast
- visible focus treatment
- touch target sizing
- non-color-only communication
- visual treatment of loading, empty, error, disabled, hover, focus, and active states

### coder owns
- state derivation
- state transitions
- runtime keyboard behavior
- focus movement driven by application state
- live-region behavior
- runtime accessibility behavior tied to business logic or app state

## Git Workflow Defaults

1. One approved plan = one working branch by default.
2. One successfully completed plan = one PR by default.
3. Planner recommends delivery shape; orchestrator decides and executes.
4. Worktrees are optional and used only when:
   - phases can run in parallel
   - file scopes do not overlap
   - separate Claude sessions are actually being used
   - added complexity is justified
5. Coder may create a checkpoint commit only when explicitly delegated by orchestrator.
6. Designer never creates branches, commits, or PRs.
7. Partial-plan PRs are not allowed unless the planner explicitly decomposes work into independently shippable plans.

## Tool and MCP Policy

| Tool / MCP | orchestrator | planner | coder | designer | Notes |
|---|---|---|---|---|---|
| Context7 | optional | use when relevant | use when relevant | use when relevant | current framework/library docs |
| GitHub MCP | optional | read-only | read-only | no | workflow/repo/PR/issue context |
| code-review-graph | no/limited | analysis | analysis + scoped mutation where allowed | analysis only | use when it adds real value |
| claude-mem | optional | use when relevant | use when relevant | use when relevant | prior project/session context |
| local repo tools | minimal | read-only only | full role-appropriate use | role-appropriate use | respect role boundaries |

## Escalation Rules

A worker must stop and report instead of guessing when:
- required scope exceeds assigned files
- ownership boundary would be crossed
- design guidance is missing for a materially visual task
- runtime behavior changes are required in a designer-owned task
- repository/worktree/git state blocks safe progress

## Delivery Shape Rules

### single-plan
Default. Use one branch and one PR for the whole approved plan.

### multi-plan
Use only when the planner explicitly determines the work contains independently reviewable and independently shippable deliverables.

## Reporting Contract
Worker completion reports should be concise by default and use this structure:

## Completion Report
Status: complete | partial | blocked

Files changed:
- ...

Validation performed:
- ...

Out-of-scope files needed:
- ...
  or
- None

Open issues:
- ...
  or
- None

Optional sections may be added only when relevant:
- External references checked
- Commit info
- States handled
- Git issues encountered