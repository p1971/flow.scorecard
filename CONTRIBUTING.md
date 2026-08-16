# Contributing

Thank you for contributing to FlowScorecard.

1. Create a focused branch from the current development branch.
2. Add tests for behavioral changes.
3. Run `dotnet build src/FlowScorecard.slnx --configuration Release`.
4. Run `dotnet test src/FlowScorecard.slnx --configuration Release`.
5. Describe the motivation, public API impact, and verification in the pull request.

All code must pass the configured .NET SDK, Roslynator, and editorconfig analysis without warnings, plus `dotnet format src/FlowScorecard.slnx --verify-no-changes`.
