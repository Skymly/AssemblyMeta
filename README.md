# AssemblyMeta

**English** | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md)

[![Verify](https://github.com/Skymly/AssemblyMeta/actions/workflows/verify.yml/badge.svg)](https://github.com/Skymly/AssemblyMeta/actions/workflows/verify.yml)
[![NuGet](https://img.shields.io/nuget/v/AssemblyMeta?label=NuGet&logo=nuget)](https://www.nuget.org/packages/AssemblyMeta)
[![GitHub Release](https://img.shields.io/github/v/release/Skymly/AssemblyMeta?label=Release)](https://github.com/Skymly/AssemblyMeta/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A take on [ThisAssembly](https://clarius.org/ThisAssembly): Roslyn incremental source generators that emit MSBuild, assembly, Git, resource, and string metadata as strongly-typed C# and Visual Basic compile-time constants — MIT-licensed with zero runtime reflection boilerplate.

```csharp
// Instead of Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyVersionAttribute>()...
// The class defaults to the RootNamespace. Top-level statements need a using for that namespace.
var version = AssemblyMeta.Info.Version;
var timeout = TimeSpan.FromSeconds(AssemblyMeta.Constants.Http.TimeoutSeconds);
```

## Installation

Install the meta-package (or any individual sub-package listed below):

```bash
dotnet add package AssemblyMeta
```

## Overview

The `AssemblyMeta` meta-package aggregates seven `IIncrementalGenerator` sub-packages (`AssemblyInfo`, `Constants`, `Project`, `Metadata`, `Git`, `Resources`, and `Strings`) for both C# and Visual Basic. Payload generators receive a single Base64 `build_property.*` payload without `AdditionalFiles`; `Strings` reads `.resx` files via `AdditionalFiles`.

- Architecture (Chinese): [docs/架构.md](docs/架构.md)
- Usage guide (Chinese): [docs/使用说明.md](docs/使用说明.md)
- End-to-end verification: `./eng/verify.ps1`
- Release history: [CHANGELOG.md](CHANGELOG.md) · Docs site build: `dotnet docfx docs/docfx.json` → `artifacts/docs`

## Differences vs ThisAssembly

Both sides are `IIncrementalGenerator`. AssemblyMeta emits C# and Visual Basic; file-based apps (`#:package`) remain C# only. The table lists real differences only; some are trade-offs rather than one-way advantages.

| Aspect | ThisAssembly | AssemblyMeta |
|---|---|---|
| License | Source MIT; since 2.1 the NuGet package carries the OSMF EULA: acceptance required at install, maintenance fee for revenue users; no technical enforcement, self-built copies unconstrained | SPDX `MIT`; no license acceptance, no fee |
| Incremental input | AdditionalTexts merged with AnalyzerConfigOptions, then ParseOptions | One payload string plus the language version; all seven generators have caching tests |
| Newlines | Generated source and multi-line constant values follow the build OS (CRLF on Windows, LF on Linux) | Always `\n`; identical output across OSes |
| Strings | Values resolved at runtime; satellite cultures supported; `{0}` and named placeholders produce parameterized methods | Compile-time constants; placeholders stay literal; satellite resx files do not participate |
| Member shape | `internal` is always `const`; `public` is a `=>` property | Non-const types become `static readonly`; under `public` everything is a field and cannot be used in attributes |
| Namespace | Both unset and blank mean global | Unset uses `RootNamespace`; only explicit blank means global |
| Git | SDK 9+ mis-strips local branch prefixes (`feature/demo` becomes `s/heads/feature/demo`); a commit hash shorter than 9 chars fails the build | Branches stripped by prefix; short hashes keep full length; also recognizes Azure DevOps and Jenkins PR numbers |
| Text resources | Default text extensions include `.resx`; `Text` then fails to find the manifest resource | `.resx` stays out of Resources by default and goes to Strings |
| Naming robustness | C# keywords are not escaped; segment conflicts throw and crash the generator | C# keywords get `@`; VB keywords use brackets; names that differ only by case report `AM0001` in VB; conflicts and renames report `AM0001`/`AM0008` |
| Diagnostics | TA001–TA003 | `AM0001`–`AM0013`, split between generator and MSBuild paths |
| Publishing | Long-lived `NUGET_API_KEY` | OIDC Trusted Publishing restricted to named pushers |

Still out of scope: F#; Strings has no localization and no parameterized formatting methods; `AssemblyMeta.Info` is fixed at nine fields while upstream collects every `Assembly*` attribute; no `Root`/`RootComment`, `Kind`, Vsix, or F#. The docs site stays Chinese; diagnostic messages are bilingual English/Chinese.

## Entry point

- `static partial class AssemblyMeta`. Uses the project's `RootNamespace` when `AssemblyMetaNamespace` is unset; explicit blank means the global namespace
- Top-level statements need a `using` for that namespace to write `AssemblyMeta.X` directly
- Each sub-package attaches nested types to that partial class (`Info`, `Constants`, `Git`, `Project`, `Metadata`, `Resources`, `Strings`)

## Packages

| Package | Purpose |
|---|---|
| `AssemblyMeta` | Meta-package aggregating the seven sub-packages below |
| `AssemblyMeta.AssemblyInfo` | Nine assembly-info constants |
| `AssemblyMeta.Constants` | `<Constant>` / `<FileConstant>` MSBuild items → strongly-typed constants |
| `AssemblyMeta.Project` | `<ProjectProperty>` opt-in project property constants |
| `AssemblyMeta.Metadata` | `<AssemblyMetadata>` item constants |
| `AssemblyMeta.Git` | Commit/Sha/Root/Url/Branch (SDK built-in SourceLink) |
| `AssemblyMeta.Resources` | `EmbeddedResource` strongly-typed access (Text/GetBytes/GetStream) |
| `AssemblyMeta.Strings` | Neutral .resx compile-time constants |

## Architecture

Each sub-package is two projects:

- `AssemblyMeta.X.csproj` — packaging: packs the analyzer dll plus props/targets under `buildTransitive`
- `AssemblyMeta.X.Generator.csproj` — netstandard2.0 analyzer referencing `Microsoft.CodeAnalysis.CSharp`

Data flow:

```
MSBuild props/targets
  → CompilerVisibleProperty / CompilerVisibleItemMetadata
  → AnalyzerConfigOptions (build_property.* / build_metadata.*)
  → IncrementalGenerator reads
  → emit C# source (AssemblyMeta partial class)
```

Key points:

- `buildTransitive` hands props and targets to the project with the `PackageReference`. The analyzer stops at that layer and does not flow to projects referencing it
- C# payload generators take one payload string plus the C# language version. VB generators read the same payload and do not read the C# language version
- C# and Visual Basic each have their own generator. There is no language-dispatch registry. `SourceEmission.Emit` still serves C# only. VB assemblies live in `analyzers/dotnet/roslyn4.11/vb`

## Testing

- Unit: Roslyn compilation assertions (`Microsoft.CodeAnalysis.CSharp` drives the generator and checks the emitted syntax trees)
- e2e: `samples/` projects run real `dotnet build` and assert constants in the compiled output

## Release & References

- Releases are tag-triggered via NuGet Trusted Publishing (OIDC); see [docs/releasing.md](docs/releasing.md).
- ThisAssembly source: [devlooped/ThisAssembly](https://github.com/devlooped/ThisAssembly)
- Key MSBuild mechanisms: `CompilerVisibleProperty`, `CompilerVisibleItemMetadata`, `EnableSourceControlManagerQueries`
