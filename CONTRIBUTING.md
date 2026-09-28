# Contributing to Deadshot Mod API

Thanks for wanting to help improve DeadshotModAPI.

## Getting Started

1. Fork and clone the repository:
   ```bash
   git clone https://github.com/DemonZ-Development/DeadshotModAPI.git
   cd DeadshotModAPI
   ```
2. Build the project:
   ```bash
   dotnet build DeadshotModAPI.csproj
   ```
3. Run the tests:
   ```bash
   dotnet test DeadshotModAPI.Tests/DeadshotModAPI.Tests.csproj
   ```

## Development Guidelines

* **Branches**: Create feature branches off `dev`, not `main`.
* **Dependencies**: Reference assemblies live in `lib/`. Keep references relative so the project builds without machine-specific paths.
* **Error Handling**: Use defensive null checks and exception handling around mod loading and IL2CPP interop. A broken mod should log the issue rather than crash the game or block other mods.
* **Code Style**: Follow standard C# conventions. Keep comments practical and direct.

## Submitting Pull Requests

1. Keep PRs focused on a single feature, bug fix, or improvement.
2. Verify the project builds with 0 errors and all tests pass.
3. Open your PR against the `dev` branch with a clear summary of what you changed.
