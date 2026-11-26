# AssemblyMeta

Roslyn source generators that emit MSBuild/assembly metadata, constants, git metadata, localized strings, and embedded resources as strongly typed C# and Visual Basic constants (`AssemblyMeta.slnx`).

## Build & Verify

```powershell
# Authoritative local & CI verification (builds, tests, and packs):
./eng/verify.ps1
```

- SDK version is pinned in [`global.json`](global.json); CI runs `./eng/verify.ps1` across Windows, Linux, and macOS.
- In CI, `verify.yml` switches to a named branch and clears `GITHUB_REF` before running `verify.ps1` so git metadata tests behave deterministically.

## Architecture & Invariants

- **`AssemblyMeta.Core`**: Holds shared models, resource/string readers, and separate emitters (`CSharpSourceEmitter`, `VisualBasicSourceEmitter`).
- **Per-domain generators**: `AssemblyInfo`, `Constants`, `Git`, `Metadata`, `Resources`, and `Strings` each split consumer runtime/MSBuild targets (`src/AssemblyMeta.<Domain>/`), C# generator, and Visual Basic generator.
- **ADR invariants** ([`docs/adr/`](docs/adr/)):
  - Unresolved git placeholders must not be written into repository properties (ADR-0001).
  - Do not introduce a language-abstraction seam over emitters (ADR-0002); keep Visual Basic as a separate emitter (ADR-0005).
  - Respect compile-time `const` vs `static readonly` rules (ADR-0003) and `.resx` delivery via `AdditionalFiles` (ADR-0004).

## Docs & Agent Config

- Domain glossary: [`CONTEXT.md`](CONTEXT.md)
- Maintainer docs: [`docs/adr/`](docs/adr/), [`docs/架构.md`](docs/架构.md), [`docs/契约.md`](docs/契约.md), [`CHANGELOG.md`](CHANGELOG.md)
- Issue routing & triage labels: [`docs/agents/`](docs/agents/)

## Conventions

- Commit messages, issues, and PR text: **English**.
- Releases are triggered on `v*` tags via `.github/workflows/release.yml`; do not commit, push, tag, or bump versions unless explicitly requested.
