---
name: watch-pr-feedback
description: Watch a specific GitHub pull request for new unresolved review comments or review threads using Monitor when available, then route to the appropriate remediation skill. Use only when the user explicitly asks to watch, monitor, wait, poll, loop, or continue handling new PR feedback as it appears.
disable-model-invocation: false
allowed-tools:
  - Bash(gh pr view *)
  - Bash(gh api *)
  - Bash(git status *)
  - Bash(git branch *)
  - Monitor
  - Skill
shell: powershell
---

# Watch PR Feedback

Watch a specific PR for new unresolved review feedback and route to remediation skills.

This skill detects and routes. It must not directly edit files, commit, push, reply, resolve threads, approve PRs, or merge PRs.

Follow:

- `agent-system-policy.md`
- `pr-review-remediation-loop.md`
- `.claude/references/github-pr-review-graphql.md`

## Invocation Boundary

Use only when the user explicitly asks to:

- watch or monitor PR comments
- wait for review feedback
- poll/check repeatedly
- keep handling feedback as it appears
- loop on Codex/human review feedback
- use Monitor for PR feedback

Do not use for one-time requests like `fix PR comment on PR #N`; use `address-pr-feedback`.

## Required Inputs

At minimum:

- PR number or PR URL

Optional:

- reviewer filter: Codex-only | all reviewers | specific author
- max watch duration
- polling interval
- max remediation cycles
- stop-on-human-reviewer-comments
- stop-on-P0/P1-findings

## Defaults

- reviewer filter: Codex-only after a Codex review request; otherwise all unresolved comments
- max remediation cycles: 3
- max speculative fix attempts per thread: 1
- max watch duration: 4 hours
- stop on user/product decision
- stop on repeated finding
- stop on unsafe git state
- do not merge PR
- do not approve PR

## Procedure

1. Confirm PR exists and is open using `gh pr view --json state --jq .state`.
2. Confirm GitHub CLI access works.
3. Confirm current branch and working tree state.
4. Start Monitor when available using one deterministic, read-only feedback-detection command based on `.claude/references/github-pr-review-graphql.md`.
5. Track seen comment/thread/review IDs in a session-local ledger.
6. When new feedback appears, classify source:
   - Codex feedback
   - human reviewer feedback
   - CI/system feedback
   - ambiguous
7. Route:
   - explicit Codex loop request → `run-codex-review-loop`
   - generic/human/ambiguous feedback → `address-pr-feedback`
8. Stop on policy stop conditions.

## Monitor Rules

Monitor commands must be:

- read-only
- deterministic
- bounded
- parser-stable
- based on `gh --json/--jq` or `gh api graphql --jq`

Do not probe or fallback through Python, Node, standalone `jq`, PowerShell, or shell translations.

If Monitor startup or parser strategy fails:

1. retry once only if transient
2. perform one manual feedback check when safe
3. report `Monitoring: not active`

Do not start a second Monitor with a different parser strategy unless the user explicitly approves.

## State Ledger

Track session-local:

- seen comment IDs
- seen review thread IDs
- comments already remediated
- comments skipped as non-actionable
- comments requiring user input
- remediation cycle count
- monitor startup status

Do not reprocess the same item unless new activity appears or the user explicitly asks to retry.

## Output

```text
Status: complete | partial | blocked

PR:
- Number:
- State:
- Branch:
- Target:

Watch:
- Mode: Monitor | scheduled | manual
- Monitoring: active | not active
- Parser: gh --jq | other-approved | unavailable
- Cycles:
- Seen comments:
- New actionable comments:

Routed:
- address-pr-feedback: [count]
- run-codex-review-loop: [count]
- None

Stopped because:
- [reason]

Next action:
- [required next step]
- None

Issues:
- [issue]
- None
```

Use the blocked report contract from `agent-system-policy.md` for blocked states.
