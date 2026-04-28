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

Watch a specific GitHub pull request for new unresolved review feedback.

This skill detects feedback and routes to remediation skills. It does not directly edit files, commit, push, reply, resolve threads, approve PRs, or merge PRs.

## Invocation Boundary

Use this skill only when the user explicitly asks to:
- watch a PR for comments
- monitor review feedback
- keep checking for new comments
- continue the review loop as comments appear
- wait for Codex review feedback
- loop until review is clean
- use Monitor for PR feedback

Do not use this skill for one-time requests like:
- "fix PR comment on PR #80"
- "address the reviewer comment on PR #80"

Use `address-pr-feedback` for one-time generic PR comment remediation.
Use `run-codex-review-loop` for explicit Codex review remediation.

## Required Inputs

At minimum:
- PR number or PR URL

Optional:
- reviewer filter: Codex-only | all reviewers | specific author
- max watch duration
- polling interval
- max remediation cycles
- whether to stop on human-reviewer comments
- whether to stop on P0/P1 findings

## Safety Defaults

If not specified:
- reviewer filter: Codex-only after a Codex review request; otherwise all unresolved comments
- max remediation cycles: 3
- max speculative fix attempts per thread: 1
- stop on user/product decision
- stop on repeated finding
- stop on unsafe git state
- do not merge PR
- do not approve PR
- do not claim active monitoring unless Monitor or another real background trigger starts successfully

## Monitor Requirement

Use `Monitor` for active watch behavior when available.

A Monitor-backed watch may run a safe GitHub CLI polling command that emits output only when relevant PR review feedback changes.

The monitored command must be read-only.

Allowed monitored actions:
- inspect PR state
- inspect unresolved review threads
- inspect PR comments
- inspect review comments
- emit newly detected comment/thread identifiers

Disallowed monitored actions:
- edit files
- commit
- push
- reply to comments
- resolve review threads
- request re-review
- approve or merge PRs

## Monitoring Truthfulness Rule

Do not say:
- "watching"
- "ping on next comment"
- "monitoring"
- "I will catch the next comment"
- "I will notify you when the next comment appears"

unless a Monitor, scheduled task, routine, channel, or other real background trigger has been successfully created.

If Monitor is unavailable, not exposed, denied, or fails to start, report:

```text
Status: complete | blocked
Mode: manual
Monitoring: not active
Next action:
- User must invoke this skill again when new feedback appears
```

Do not imply ongoing background work is happening in manual mode.

## Monitor Startup

When active monitoring is requested:

1. Confirm the PR exists.
2. Confirm the PR is open.
3. Confirm GitHub CLI access works.
4. Confirm the current branch and working tree state.
5. Start Monitor with a read-only feedback-detection command.
6. Report whether monitoring started successfully.

If Monitor startup fails:
1. retry once if the failure appears transient
2. fall back to one manual feedback check
3. report `Monitoring: not active`

## Suggested Monitor Command Shape

The exact command may vary by shell and repository, but it must be read-only and should emit stable identifiers for newly observed feedback.

Conceptual shape:

```powershell
while ($true) {
  gh api graphql `
    -f owner="OWNER" `
    -f repo="REPO" `
    -F pr=123 `
    -f query='
query($owner: String!, $repo: String!, $pr: Int!) {
  repository(owner: $owner, name: $repo) {
    pullRequest(number: $pr) {
      state
      reviewThreads(first: 100) {
        nodes {
          id
          isResolved
          isOutdated
          path
          line
          comments(first: 20) {
            nodes {
              id
              author { login }
              body
              createdAt
              url
            }
          }
        }
      }
      comments(first: 100) {
        nodes {
          id
          author { login }
          body
          createdAt
          url
        }
      }
      reviews(first: 100) {
        nodes {
          id
          author { login }
          state
          body
          createdAt
        }
      }
    }
  }
}'
  Start-Sleep -Seconds 60
}
```

Prefer the GraphQL operations documented in `github-pr-review-graphql.md` when this skill is colocated with that reference, or in the repository's review GraphQL reference if stored elsewhere.

## State Tracking

Maintain a session-local ledger of:
- seen comment IDs
- seen review thread IDs
- comments already remediated
- comments skipped as non-actionable
- comments requiring user input
- remediation cycle count
- monitor startup status

Do not reprocess the same comment unless:
- the thread received a new reply
- the comment became unresolved again
- the user explicitly asks to retry

## Feedback Fetch

Use GitHub CLI / GraphQL to fetch:
- PR state
- unresolved review threads
- review comments
- top-level PR comments
- review summaries when available

Classify new feedback as:
- Codex feedback
- human reviewer feedback
- CI/system feedback
- ambiguous

## Routing

For new Codex review feedback:
- invoke `run-codex-review-loop` only if the user requested Codex loop behavior
- otherwise invoke `address-pr-feedback`

For generic or human reviewer feedback:
- invoke `address-pr-feedback`

If multiple unrelated comments arrive at once:
- group them into one remediation batch only when safe
- otherwise stop and summarize candidate items

## Stop Conditions

Stop when:
- PR is merged or closed
- no new actionable feedback appears before the watch limit
- max remediation cycles is reached
- feedback requires user input
- feedback requires an out-of-scope architecture/product decision
- the same finding repeats after attempted remediation
- unsafe git state is detected
- remediation skill returns blocked
- GitHub API access fails after one retry
- Monitor fails or exits unexpectedly and no safe fallback exists

## Failure Contract

This skill must not crash or wait silently.

If a fetch, parse, skill invocation, Monitor startup, Monitor runtime, or GitHub API call fails:
1. retry once if transient
2. use a safe fallback when available
3. otherwise return blocked

Blocked format:

```text
Status: blocked
Stage: watch | monitor-start | monitor-runtime | fetch | classify | route | remediation
Blocker: [one-line reason]
Retry status: [not attempted | retried once | exhausted]
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
- Mode: Monitor | scheduled | manual
- Monitoring: active | not active
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
