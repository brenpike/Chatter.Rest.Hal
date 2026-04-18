CONTRIBUTING

Thank you for considering a contribution to Chatter.Rest.Hal 🎉

This document describes how to get started, the expectations for contributions, and the guidance to prepare a clean, reviewable pull request.

1. Who this is for

- New contributors who want to build, run tests, or extend the project.
- Maintainers who need a consistent contribution workflow.

2. Getting started (high level)

- Prerequisites: Install the .NET SDK version defined in global.json (use the LTS recommended by the repo maintainers), and a code editor (Visual Studio, VS Code, Rider).
- Clone the repository and create a feature branch from main/master. Use descriptive branch names (e.g., feat/add-http-links, fix/generator-nullref).
- Run the project build and tests locally before opening a PR. See docs/development.md for development details and how to run the generator.

3. Code style and quality expectations

- Follow standard C# conventions (naming, file organization) and the repository's editorconfig if present.
- Keep changes small and focused. One logical change per pull request helps reviewers.
- Add or update unit tests for behavioral changes. Tests that demonstrate the bug and pass after the fix are preferred.
- Run the formatter and static analysis before committing. If you use an IDE, enable format-on-save or run dotnet format against the solution.

4. Tests

- All unit tests must pass locally and in CI.
- Include tests for new behavior and edge cases when relevant.
- If a change temporarily requires a failing test (e.g. reproducing a bug), mark it with a clear comment and a linked issue. Prefer fixing the issue in the same PR.

5. Commit messages

- Use clear, imperative-style commit messages (e.g., "Add HAL link helper", "Fix null ref in generator").
- Squash or tidy up WIP commits before the final PR if appropriate.

6. Pull request guidance

- Base your PR on the default development branch (main or master — follow repository convention).
- Title and description: include a short summary, motivation, and any design notes. Link to related issues using Issue # numbers when available.
- Make it easy to review: include screenshots (UI changes), relevant logs, and a short checklist of what you verified locally.
- Add unit/integration tests or explain why tests are not necessary.
- Tag one or more maintainers as reviewers (see repository owner or CODEOWNERS if present).

7. Code generation and schema changes

- This project uses a code-generation tool for some artifacts. Changes that affect generated sources must include either:
  - regenerated outputs in the PR, or
  - clear instructions for maintainers on how to regenerate outputs (see docs/development.md).
- Keep generated files separate from handwritten code where possible.

8. Security and secrets

- Do not commit secrets, credentials, or any private keys. If your change requires secret configuration for local testing, document how to use environment variables or a local secrets store.

9. Licensing and contributor agreement

- By contributing, you confirm your code can be licensed under this repository's license. Do not submit third-party code without proper attribution and license compatibility.

10. Getting help

- Open an issue if you're unsure where to start or want to discuss a design before implementing it. Maintainers will triage and guide.

Thanks again — we appreciate your help in improving Chatter.Rest.Hal.
