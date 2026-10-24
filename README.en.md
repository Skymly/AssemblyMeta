# AssemblyMeta

[中文](README.md) | English

A take on [ThisAssembly](https://clarius.org/ThisAssembly): generates project/assembly metadata as strongly-typed constants at compile time (Roslyn Source Generator), removing runtime reflection boilerplate.

```csharp
// Instead of Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyVersionAttribute>()...
// The class defaults to the RootNamespace. Top-level statements need a using for that namespace.
var version = AssemblyMeta.Info.Version;
var timeout = TimeSpan.FromSeconds(AssemblyMeta.Constants.Http.TimeoutSeconds);
```

- License: MIT (free, no maintenance-fee model)
- NuGet: [`AssemblyMeta`](https://www.nuget.org/packages/AssemblyMeta) (Trusted Publishing, tag-triggered releases)
- Repository: [`Skymly/AssemblyMeta`](https://github.com/Skymly/AssemblyMeta)


## Status

The behavior of 1.0.0 is recorded in [CHANGELOG.md](CHANGELOG.md). The meta-package aggregates AssemblyInfo, Constants, Project, Metadata, Git, Resources and Strings. Generators are `IIncrementalGenerator`; data flows in as a single Base64 payload via `build_property.*`, without `AdditionalFiles`. The docs site builds with `dotnet docfx docs/docfx.json` into `artifacts/docs`.

- Architecture (Chinese): [docs/架构.md](docs/架构.md)
- Usage guide (Chinese): [docs/使用说明.md](docs/使用说明.md)
- End-to-end verification: `./eng/verify.ps1`

## Release

The package version lives only in `Directory.Build.props` as `AssemblyMetaPackageVersion`. Push a tag `v` plus that version. The current version is `1.0.0`, so push `v1.0.0`.

`.github/workflows/release.yml` runs `./eng/verify.ps1` on that tag, then publishes to `https://api.nuget.org/v3/index.json` via NuGet Trusted Publishing. The seven sub-packages go first, then the meta-package, then a GitHub Release is created from the matching `CHANGELOG.md` section. No long-lived API key is used and no NuGet secret lives in the repo. Only tags pushed by `Skymly` or `wys0610` publish; agent-pushed tags resolve via `github.triggering_actor`. Tags from anyone else fail the release instead of being silently skipped.

The nuget.org policy trusts the `Skymly/AssemblyMeta` repository, the `release.yml` workflow filename, package owner `Skym`, and package names `AssemblyMeta*`. The job must not add a GitHub Environment — the policy has none. A successful publish is required within 7 days of policy creation for it to become permanent.

## Differences vs ThisAssembly

Both sides are `IIncrementalGenerator`, emit C# only, and support file-based apps (`#:package`). The table lists real differences only; some are trade-offs rather than one-way advantages.

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
| Naming robustness | C# keywords are not escaped; segment conflicts throw and crash the generator | Keywords get `@`; conflicts and renames report `AM0001`/`AM0008` |
| Diagnostics | TA001–TA003 | `AM0001`–`AM0011`, split between generator and MSBuild paths |
| Publishing | Long-lived `NUGET_API_KEY` | OIDC Trusted Publishing restricted to named pushers |

AssemblyMeta's gaps: Strings has no localization and no parameterized formatting methods; `AssemblyMeta.Info` is fixed at nine fields while upstream collects every `Assembly*` attribute; no `Root`/`RootComment`, `Kind`, or Vsix; documentation and diagnostics are primarily Chinese (diagnostic messages are bilingual English/Chinese).

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
- Each generator's incremental input is one payload string plus the C# language version
- C# only; no per-language emitter dispatch

## Testing

- Unit: Roslyn compilation assertions (`Microsoft.CodeAnalysis.CSharp` drives the generator and checks the emitted syntax trees)
- e2e: `samples/` projects run real `dotnet build` and assert constants in the compiled output

## References

- ThisAssembly source: [devlooped/ThisAssembly](https://github.com/devlooped/ThisAssembly)
- Key MSBuild mechanisms: `CompilerVisibleProperty`, `CompilerVisibleItemMetadata`, `EnableSourceControlManagerQueries`
