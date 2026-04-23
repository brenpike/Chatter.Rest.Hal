# Contributing

Contributions are welcome. For non-trivial changes, please open an issue first to discuss what you would like to change.

## Reporting Issues

Use [GitHub Issues](https://github.com/brenpike/Chatter.Rest.Hal/issues) to report bugs or request features. Please include:

- .NET SDK version
- Package version
- A minimal reproduction
- Actual vs expected behavior

## Getting Started

Prerequisites:

- .NET 8.0 SDK (8.0.x)
- No other tools required

Clone and build:

```bash
git clone https://github.com/brenpike/Chatter.Rest.Hal.git
cd Chatter.Rest.Hal
dotnet restore
dotnet build
```

Run tests:

```bash
dotnet test
```

Full details on build, test, and pack commands are in [docs/development.md](docs/development.md).

## Making Changes

Branch naming:

- `feature/<short-description>` for new features
- `bugfix/<short-description>` for bug fixes
- `docs/<short-description>` for documentation
- `refactor/<short-description>` for refactoring

Always branch from `main` and submit pull requests back to `main`.

## Code Style

Follow the existing `.editorconfig` rules:

- Indentation: tabs
- Line endings: CRLF
- Braces: Allman style
- Nullable: enabled

Run `dotnet build` before submitting - zero warnings expected on `net8.0`.

## Tests

- All new behavior must include tests
- Framework: xunit 2.4.x with FluentAssertions 6.x
- Test naming: `Method_Scenario_Expected` (example: `AddLink_WithDuplicateRelation_ThrowsInvalidOperationException`)
- JSON fixtures go in `test/Chatter.Rest.Hal.Tests/Json/`
- Use `TestHelpers` factory methods for common setup

## Pull Request Checklist

- [ ] Branched from `main`
- [ ] `dotnet build` passes with zero warnings on `net8.0`
- [ ] `dotnet test` passes
- [ ] New behavior has test coverage
- [ ] Code style matches `.editorconfig`
- [ ] PR description explains the why, not just the what

## License

By contributing, you agree your contributions will be licensed under the project's [MIT License](LICENSE).
