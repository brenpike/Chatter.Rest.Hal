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
query($owner: String!, $repo: String!, $pr: Int!, $after: String) {
  repository(owner: $owner, name: $repo) {
    pullRequest(number: $pr) {
      number
      url
      reviewThreads(first: 100, after: $after) {
        pageInfo {
          hasNextPage
          endCursor
        }
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

If `pageInfo.hasNextPage` is `true`, repeat the query with `-F after="END_CURSOR"` (using the `endCursor` value) until `hasNextPage` is `false`.

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
query($owner: String!, $repo: String!, $pr: Int!, $after: String) {
  repository(owner: $owner, name: $repo) {
    pullRequest(number: $pr) {
      comments(first: 100, after: $after) {
        pageInfo {
          hasNextPage
          endCursor
        }
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

If `pageInfo.hasNextPage` is `true`, repeat the query with `-F after="END_CURSOR"` (using the `endCursor` value) until `hasNextPage` is `false`.

## Fetch review summaries

Review summaries are distinct from inline review thread comments. A `PullRequestReview` contains a top-level `body` submitted alongside the review verdict. These are accessed through the `reviews` connection on a pull request, not through `reviewThreads` or `comments`. The `state` field indicates the review verdict: `APPROVED`, `CHANGES_REQUESTED`, `COMMENTED`, or `DISMISSED`.

```powershell
gh api graphql `
  -f owner="OWNER" `
  -f repo="REPO" `
  -F pr=123 `
  -f query='
query($owner: String!, $repo: String!, $pr: Int!) {
  repository(owner: $owner, name: $repo) {
    pullRequest(number: $pr) {
      reviews(first: 100) {
        pageInfo {
          hasNextPage
          endCursor
        }
        nodes {
          id
          author {
            login
          }
          body
          state
          submittedAt
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
