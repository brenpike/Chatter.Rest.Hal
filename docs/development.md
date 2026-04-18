Development — Local setup, tests, and code generation

Purpose

This document explains the developer workflows used in this repository: how to set up a local environment, run builds and tests, and run the code generator(s). It's written for new contributors who want to iterate on code and verify changes locally.

Prerequisites

- Install the .NET SDK version referenced by global.json in the repository root. Use the SDK that matches that file to avoid compatibility issues.
- A modern code editor or IDE (Visual Studio, VS Code, Rider) with C# tooling is recommended.
- Git for cloning and branching.

Repository layout (high-level)

- src/ — main source projects
- test/ — unit and integration tests
- docs/ — repository documentation (this file)
- generator or tools/ (if present) — code-generation tool projects

Note: exact directory names may vary; search the repo for project names or for a generator-related project if you are unsure.

Common development workflows

1) Build and run tests

Outcome: you will verify that the solution builds and that all tests pass locally.
What to do (outcome-focused):
- Restore dependencies and build the solution using your .NET tooling.
- Execute the test suites (unit tests first; integration tests as needed).
- Fix any failing tests or open an issue if a failing test represents an unrelated CI/regression problem.

What to expect:
- A clean build and passing tests should match CI results. If tests fail locally but pass in CI, confirm SDK versions and environmental differences.

2) Running the code generator (high-level)

Outcome: regenerate generated artifacts used by the project (models, clients, or other assets produced by a generator).

How to proceed (conceptual steps):
- Identify the generator project in the repository (commonly found under a tools/ or src/ directory). The project name or README often indicates it is the generator.
- Build the generator project with the same .NET SDK used for the main solution.
- Run the generator with the expected input files (e.g., schema, templates, or configuration). The generator usually accepts input path(s) and an output directory. When in doubt, open the generator project's README or source code to confirm expected arguments.
- Inspect the generated output and add the regenerated files to your branch if your change requires updated generated code.

Notes and best practices:
- Prefer regenerating outputs locally and including the results in the same PR when changing inputs that affect generated files. This keeps CI reproducible and reviewers able to verify the final state.
- If generated code is large, consider including only the meaningful diffs or explain why generated output is excluded.

3) Debugging and iterative development

Outcome: quickly iterate on a change with fast feedback.

What to do:
- Use your IDE debugger to step through code and tests.
- Add focused unit tests to reproduce bugs before implementing fixes.
- Keep iterations small; run relevant tests frequently rather than the entire suite on every change.

Troubleshooting tips

- SDK mismatch: If build/test failures indicate SDK differences, confirm the version in global.json and install that SDK.
- Missing tools: If the generator requires an external tool or NuGet package, the generator project's README should list them. Install or restore as instructed.
- Environmental differences: CI may run with different environment variables or OS constraints. When behavior diverges, capture logs and open an issue with reproduction steps.

What to include in a development PR

- A short summary of the change and why it was made.
- Steps you followed to validate the change locally (build, test, generate outputs).
- Any files that were regenerated as part of the change, or explicit instructions to regenerate them.
- Notes on compatibility or versioning if the change affects public APIs or generated artifacts.

Further reading and reference

- See CONTRIBUTING.md for repository-level contribution expectations and PR guidance.
- The repository's README may hold additional high-level info.

If something in these docs is unclear or missing, please open an issue titled "docs: clarify development workflow" and describe what you'd like to see.