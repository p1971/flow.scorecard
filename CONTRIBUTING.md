# Contributing

Thank you for contributing to Flow.Scorecard.

1. Create a focused branch from the current development branch.
2. Add tests for behavioral changes.
3. Run `dotnet build src/Flow.Scorecard.slnx --configuration Release`.
4. Run `dotnet test src/Flow.Scorecard.slnx --configuration Release`.
5. Describe the motivation, public API impact, and verification in the pull request.

All code must pass the configured .NET SDK, Roslynator, and editorconfig analysis without warnings, plus `dotnet format src/Flow.Scorecard.slnx --verify-no-changes`.

## Releasing

GitVersion calculates build and package versions from the repository history. To publish a package to NuGet.org:

1. Create a GitHub Release for the commit to publish, using a semantic-version tag such as `v1.2.3` (or `v1.2.3-beta.1` for a prerelease).
2. Publish the GitHub Release.

Publishing the release runs the full build and test workflow before packing and pushing the package. Branch pushes, pull requests, and tags without a published GitHub Release never publish packages.
