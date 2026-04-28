---
name: watch-pr-feedback
description: Watch a specific GitHub pull request for new unresolved review comments or review threads using Claude Code dynamic /loop Monitor behavior, then route actionable feedback to the appropriate remediation skill. Use only when the user explicitly asks to watch, monitor, wait for, loop on, or continue handling PR feedback as it appears.
disable-model-invocation: false
allowed-tools:
  - Bash(gh pr view *)
  - Bash(gh api *)
  - Bash(git status *)
  - Bash(git branch *)
  - Bash(git rev-parse *)
  - Skill
shell: powershell
---

# Watch PR Feedback

Watch a specific GitHub pull request for new unresolved review feedback, using Claude Code dynamic `/loop` / Monitor behavior as the preferred wake mechanism.

This skill detects feedback and routes work. It must not directly edit files, commit, push, reply to comments, resolve review threads, or request re-review.

## Invocation Boundary

Use this skill only when the user explicitly asks to:
- watch a PR for comments
- monitor review feedback
- wait for review feedback
- keep checking for new comments
- continue handling PR feedback as it appears
- loop until PR review is clean
- wait for Codex review feedback
- monitor Codex review feedback

Do not use this skill for one-time requests such as:
- `fix PR comment on PR #80`
- `address the reviewer comment on PR #80`

Use `remediate-pr-comment` for one-time generic PR comment remediation.
Use `remediate-codex-review` for an explicit Codex review remediation pass or Codex re-review loop.

## Preferred Monitor Usage

Prefer a dynamic `/loop` invocation with no fixed interval so Claude Code can use Monitor when available.

Recommended user-facing invocation patterns:

```text
/loop /watch-pr-feedback PR #<number> Codex-only max 3 cycles
```

```text
/loop watch PR #<number> for new review feedback and route actionable comments through the appropriate remediation skill. Use Monitor when available. Stop when clean, blocked, merged, closed, or after 3 remediation cycles.
```

Monitor is preferred because it can wait on a background signal instead of repeatedly re-running a full polling prompt. If Monitor is unavailable in the current Claude Code environment, use the normal dynamic `/loop` cadence as fallback. If neither Monitor nor `/loop` scheduling is available, return `blocked` and recommend manual one-shot remediation.

## Inputs

Required:
- PR number or PR URL

Optional:
- reviewer filter: Codex-only, human-only, all reviewers
- max remediation cycles
- max watch duration
- whether to stop on human-reviewer comments
- whether to stop on P0/P1 findings
- whether to request Codex re-review after remediation

## Safety Defaults

If not specified:
- reviewer filter: Codex-only when the user explicitly mentions Codex; otherwise all unresolved review feedback
- max remediation cycles: 3
- max speculative fix attempts per thread: 1
- stop on user/product decision
- stop on repeated finding
- stop on unsafe git state
- stop when PR is merged or closed
- do not merge PR
- do not approve PR
- do not create new branches unless the invoked remediation skill requires normal branch preflight

## State Tracking

Maintain a session-local watch ledger of:
- PR number and URL
- PR head branch and target branch
- seen comment IDs
- seen review thread IDs
- comments already routed
- comments skipped as non-actionable
- comments requiring user input
- remediation cycle count
- last observed review activity timestamp

Do not reprocess the same comment or thread unless:
- the thread received a new reply
- the thread became unresolved again
- the user explicitly asks to retry

The watch ledger is a session artifact by default. Do not commit it.

## Feedback Fetch

Use GitHub CLI / GraphQL to fetch:
- PR state
- PR head branch
- unresolved review threads
- inline review comments
- top-level PR comments
- review summaries

Classify new feedback as:
- Codex feedback
- human reviewer feedback
- CI/system feedback
- ambiguous feedback

Do not silently ignore new feedback.

## Routing

For new Codex review feedback:
- invoke `remediate-codex-review` only when the user requested Codex loop behavior
- otherwise invoke `remediate-pr-comment`

For generic or human reviewer feedback:
- invoke `remediate-pr-comment`

For CI/system feedback:
- return `blocked` unless the current user request explicitly includes CI remediation

If multiple unrelated comments arrive at once:
- batch them only when they affect the same area and can be remediated safely together
- otherwise return `blocked` with a concise candidate list and ask the orchestrator/user to choose batching or ordering

## Stop Conditions

Stop when any of the following is true:
- PR is merged or closed
- no new actionable feedback appears before the watch limit
- max remediation cycles is reached
- feedback requires user input
- feedback requires an out-of-scope architecture/product/public API/versioning/release decision
- the same finding repeats after attempted remediation
- unsafe git state is detected
- remediation skill returns `blocked`
- GitHub API access fails after one retry
- Monitor or scheduled loop support is unavailable and manual fallback is required

## Failure Contract

This skill must not crash, wait silently, or leave a watch loop unresolved.

If a fetch, parse, Monitor wake, scheduling step, skill invocation, or GitHub API call fails:
1. retry once if the failure appears transient
2. use a safe fallback when available
3. otherwise return `blocked`

Blocked format:

```text
Status: blocked
Stage: watch | monitor | fetch | classify | route | remediation | git
Blocker: [one-line reason]
Retry status: [not attempted | retried once | exhausted]
Fallback used: [none | description]
Impact: [what cannot proceed]
Next action:
- [specific next step]
```

## Output

```text
Status: complete | partial | blocked

PR:
- Number:
- State:
- Branch:
- Target:

Watch:
- Mode: Monitor | dynamic loop | fixed loop | manual fallback
- Cycles:
- Seen comments:
- New actionable comments:

Routed:
- remediate-pr-comment: [count]
- remediate-codex-review: [count]
- None

Stopped because:
- [reason]

Issues:
- [issue]
- None
```
