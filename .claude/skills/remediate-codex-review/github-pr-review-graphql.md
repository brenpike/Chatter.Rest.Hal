# GitHub PR Review GraphQL Reference

Use these GraphQL operations for pull request review remediation.

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
              author {
                login
              }
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

> **Note on thread comment pagination:** The `comments` connection in the batch reviewThreads query uses a fixed `first: 20` cap. Comment cursors are connection-specific and cannot be shared across threads in a single query. To paginate comments beyond the first 20 for a specific thread, issue a separate query for that thread by ID using the GitHub GraphQL `node` root field and its `comments` connection with its own `$after` cursor.

## Reply to a review thread

```powershell
gh api graphql `
  -f threadId="THREAD_ID" `
  -f body="Fixed in COMMIT_SHA. Summary: ..." `
  -f query='
mutation($threadId: ID!, $body: String!) {
  addPullRequestReviewThreadReply(
    input: {
      pullRequestReviewThreadId: $threadId,
      body: $body
    }
  ) {
    comment {
      id
      url
    }
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
    thread {
      id
      isResolved
    }
  }
}'
```

## Fetch top-level PR comments

Top-level PR comments are issue comments because every PR is also an issue. Use `gh pr view` or GitHub GraphQL issue comments when needed.

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
          author {
            login
          }
          body
          createdAt
          url
        }
      }
    }
  }
}'
```

## Author Filtering

When processing Codex-only feedback, include comments whose author login matches the repository's Codex reviewer identity. If the identity is unclear, report the candidate authors and ask the user before processing non-human or ambiguous reviewers.

## Safety Rules

- Resolve only threads that were actually fixed, pushed, and validated.
- Reply before resolving.
- Include commit SHA in the reply when a code change was made.
- Do not resolve unresolved questions.
- Do not resolve rejected P0/P1, security, public API, compatibility, versioning, or release feedback without user approval unless project policy explicitly permits it.
