# GhostFx Developer & Agent Guidelines

Welcome to the **GhostFx** repository! This document provides operational guidelines, project structure details, build/test commands, and coding standards for AI agents and human developers working in this workspace.

---

## 1. Project Overview & Architecture

**GhostFx** is a .NET 10 solution and CLI tool designed to migrate Ghost blogs (from live Admin API endpoints or exported JSON files) into Git-versioned Markdown content and DocFX static site generation structures.

### Component Map
- **`GhostFx.slnx`**: Modern XML solution file referencing all projects.
- **`Directory.Build.props`**: Centralized MSBuild properties (`TargetFramework`, `ImplicitUsings`, `Nullable`).
- **`Directory.Packages.props`**: Central Package Management (CPM) governing all NuGet package versions.
- **`GhostFx.Core/`** (`net10.0`): Core class library containing migration logic.
  - `MigrationEngine.cs`: Orchestrates fetching, converting, and writing DocFX files.
  - `GhostAdminClient.cs`: Handles JWT authentication and communication with Ghost Admin API v3/v4/v5.
  - `GhostJsonParser.cs`: Parses offline Ghost JSON export structures.
  - `MarkdownConverter.cs`: HTML-to-Markdown conversion powered by `ReverseMarkdown` and YAML front-matter generation via `YamlDotNet`.
  - `DocfxGenerator.cs`: Generates `docfx.json`, `index.md`, `toc.yml`, and tag indices.
  - `GhostFxConfig.cs`: Configuration options schema (`ghostfx.json`).
- **`GhostFx.Cli/`** (`net10.0`): Command-line application using `System.CommandLine`. Supports quiet mode (`--quiet`, `-q`), piped stdin/stdout/stderr workflows, non-interactive execution, and cross-platform exit code handling. Packable as a global .NET tool (`ghostfx`).
- **`GhostFx.Tests/`** (`net10.0`): xUnit unit test suite for configuration, parsing, markdown conversion, media path normalization, quiet mode, and migration workflows.

---

## 2. Build & Test Instructions

### Build Solution
```bash
dotnet build GhostFx.slnx
```

### Run Unit Tests
```bash
dotnet test GhostFx.slnx
```

### Run CLI Locally
```bash
dotnet run --project GhostFx.Cli/GhostFx.Cli.csproj -- --help
```

---

## 3. Key Conventions & Guidelines

1. **Target Framework**: All projects target `.NET 10.0` (`net10.0`). Do not downgrade target frameworks.
2. **Solution Format**: The solution uses `.slnx` format (`GhostFx.slnx`). Do not commit legacy `.sln` files.
3. **C# Standards**:
   - Enable C# 13/14 language features (`ImplicitUsings: enable`, `Nullable: enable`).
   - Use primary constructors and file-scoped namespaces (`namespace GhostFx.Core;`).
   - Use `System.Text.Json` for JSON processing and `YamlDotNet` for YAML front-matter formatting.
4. **Testing**:
   - Unit tests are located in `GhostFx.Tests`.
   - Maintain unit test coverage for any changes to `MarkdownConverter`, `GhostJsonParser`, `DocfxGenerator`, or `MediaDownloader`.
   - Test methods must be attributed with `[Fact]` or `[Theory]` and include `using Xunit;`.
5. **CLI & I/O Standards**:
   - Support `--quiet` / `-q` mode and automatic stdout redirection detection (`Console.IsOutputRedirected`).
   - Support piped standard input (`Console.In` / `Console.IsInputRedirected`) for JSON exports or configurations.
   - Route warnings and errors to standard error (`Console.Error`).
   - Use cross-platform exit codes (`0` for success, `1` for error) across Windows, Linux, and macOS.

---

## 4. Configuration File (`ghostfx.json`) Schema

```json
{
  "ghostUrl": "https://yourblog.ghost.io",
  "adminApiKey": "YOUR_ADMIN_API_KEY_ID:YOUR_ADMIN_API_KEY_SECRET",
  "ghostExportJson": "sample-ghost-export.json",
  "outputDir": "articles",
  "indexFile": "index.md",
  "siteTitle": "My Migrated Ghost Blog",
  "includeDrafts": false,
  "downloadTheme": false,
  "themePath": "ghostfx.zip",
  "quiet": false,
  "logoPath": true,
  "excerptMaxLength": 190
}
```

---

## 5. Output Directory Structure

Generated static site source files are organized under `outputDir`:

```
<outputDir>/
├── docfx.json
├── index.md
├── tags.md
├── toc.yml
├── published/         # Published posts
│   └── post-slug.md
├── pages/             # Ghost pages
│   └── page-slug.md
├── draft/             # Draft posts
│   └── draft-slug.md
├── scheduled/         # Scheduled posts
│   └── post-slug.md
├── content/
│   └── images/        # Backwards-compatible Ghost media & brand assets
│       └── 2024/05/sample.jpg
└── template/
    └── ghostfx/       # Converted DocFX modern theme overrides
```
