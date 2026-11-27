# Release Process

The package version lives only in `Directory.Build.props` as `<AssemblyMetaPackageVersion>`.

To publish a release, push an annotated tag `v<version>` (for example, `v1.2.0` when `<AssemblyMetaPackageVersion>` is `1.2.0`).

`.github/workflows/release.yml` runs `./eng/verify.ps1` on the tag and publishes via NuGet Trusted Publishing (OIDC) to `https://api.nuget.org/v3/index.json`:

1. The seven sub-packages are pushed first, followed by the `AssemblyMeta` meta-package.
2. A GitHub Release is created from the matching section in `CHANGELOG.md`.
3. Only tags pushed by authorized maintainers (`Skymly` or `wys0610`, resolving agent-pushed tags via `github.triggering_actor`) are permitted; unauthorized tag pushes fail closed.

The nuget.org Trusted Publishing policy binds to repository `Skymly/AssemblyMeta`, workflow `release.yml`, owner `Skym`, and package prefix `AssemblyMeta*` without a GitHub Environment.
