# GitHub PR Review GraphQL Reference

Use these GraphQL operations for pull request review remediation.

Resolvable pull request review threads are GraphQL objects. Do not try to resolve review threads using REST review-comment IDs.

## Fetch review threads

```bash
gh api graphql \
  -f owner="OWNER" \
  -f repo="REPO" \
  -F pr=123 \
  -f query='
query($owner: String!, $repo: String!, $pr: Int!, $threadCursor: String) {
  repository(owner: $owner, name: $repo) {
    pullRequest(number: $pr) {
      number
      url
      reviewThreads(first: 100, after: $threadCursor) {
        pageInfo { hasNextPage endCursor }
        nodes {
          id
          isResolved
          isOutdated
          path
          line
          comments(first: 20) {
            pageInfo { hasNextPage endCursor }
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

## Fetch comments for a review thread

```bash
gh api graphql \
  -f threadId="THREAD_ID" \
  -f query='
query($threadId: ID!, $cursor: String) {
  node(id: $threadId) {
    ... on PullRequestReviewThread {
      comments(first: 20, after: $cursor) {
        pageInfo { hasNextPage endCursor }
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
}'
```

## Reply to a review thread

```bash
gh api graphql \
  -f threadId="THREAD_ID" \
  -f body="Fixed in COMMIT_SHA. Summary: ..." \
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

```bash
gh api graphql \
  -f threadId="THREAD_ID" \
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

```bash
gh api graphql \
  -f owner="OWNER" \
  -f repo="REPO" \
  -F pr=123 \
  -f query='
query($owner: String!, $repo: String!, $pr: Int!, $commentCursor: String) {
  repository(owner: $owner, name: $repo) {
    pullRequest(number: $pr) {
      comments(first: 100, after: $commentCursor) {
        pageInfo { hasNextPage endCursor }
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

## Pagination

### Review threads
Paginate `reviewThreads` using `$threadCursor` until `pageInfo.hasNextPage` is `false`. Accumulate all thread nodes before classifying.

### Thread comments
The initial thread fetch retrieves up to 20 comments per thread inline. If a thread's `comments.pageInfo.hasNextPage` is `true`, use **Fetch comments for a review thread** with that thread's node ID and `comments.pageInfo.endCursor` to retrieve subsequent pages. Each thread requires its own cursor — do not reuse cursors across threads.

### Top-level PR comments
Paginate `comments` using `$commentCursor` until `pageInfo.hasNextPage` is `false`. Accumulate all comment nodes before classifying.

### General rule
Do not classify or route feedback from a partial (first-page-only) result set.

## Author Filtering

When processing Codex-only feedback, include comments whose author login matches the repository's Codex reviewer identity. If the identity is unclear, report the candidate authors and ask the user before processing non-human or ambiguous reviewers.

## Safety Rules

- Resolve only threads that were actually fixed, pushed, and validated.
- Reply before resolving.
- Include commit SHA in the reply when a code change was made.
- Do not resolve unresolved questions.
- Do not resolve rejected P0/P1, security, public API, compatibility, versioning, or release feedback without user approval unless project policy explicitly permits it.
