# GitHub PR Review GraphQL Reference

Use these GraphQL operations through `gh api graphql` for Codex PR review remediation.

Resolvable pull request review threads are GraphQL objects. Do not try to resolve review threads using REST review-comment IDs.

## Fetch review threads

```powershell
gh api graphql `
  -f owner="OWNER" `
  -f repo="REPO" `
  -F pr=123 `
  -f query='
query($owner: String!, $repo: String!, $pr: Int!) {
  repository(owner: $owner, name: $repo) {
    pullRequest(number: $pr) {
      number
      url
      headRefName
      baseRefName
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
              path
              line
              diffHunk
            }
          }
        }
      }
    }
  }
}'
```

## Reply to a review thread

```powershell
gh api graphql `
  -f threadId="THREAD_ID" `
  -f body="Fixed in COMMIT_SHA. Summary: ..." `
  -f query='
mutation($threadId: ID!, $body: String!) {
  addPullRequestReviewThreadReply(
    input: { pullRequestReviewThreadId: $threadId, body: $body }
  ) {
    comment { id url }
  }
}'
```

## Resolve a review thread

```powershell
gh api graphql `
  -f threadId="THREAD_ID" `
  -f query='
mutation($threadId: ID!) {
  resolveReviewThread(input: { threadId: $threadId }) {
    thread { id isResolved }
  }
}'
```

## Fetch top-level PR comments

Top-level PR comments are issue comments because every PR is also an issue. Use this when Codex leaves non-inline comments.

```powershell
gh api graphql `
  -f owner="OWNER" `
  -f repo="REPO" `
  -F pr=123 `
  -f query='
query($owner: String!, $repo: String!, $pr: Int!) {
  repository(owner: $owner, name: $repo) {
    pullRequest(number: $pr) {
      comments(first: 100) {
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
}'
```

## Request Codex re-review

```powershell
gh pr comment 123 --body "@codex review the latest changes and verify the prior findings were addressed. Focus only on remaining regressions, missing tests, public API compatibility, security issues, package behavior, versioning/SemVer issues, and HAL/HATEOAS behavior."
```

## Safety rules

- Resolve only after the fix is committed, pushed, and validated.
- Include the commit SHA in the reply.
- Do not resolve unresolved questions.
- Do not repeatedly request re-review without new commits or a written rationale.
