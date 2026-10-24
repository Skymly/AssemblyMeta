# AssemblyMeta.Git

Generates the current repository's git information as `AssemblyMeta.Git` constants.

- `Commit`: taken verbatim from `RepositoryCommit` when set, otherwise SourceLink's `SourceRevisionId`
- `Sha`: taken verbatim from `RepositorySha` when set, otherwise the first 9 characters of `Commit`, or the full length when shorter
- `Root`: repository root with forward slashes, ending in a slash when non-empty
- `Url`: only filled when `PublishRepositoryUrl=true` or `RepositoryUrl` is already set
- `Branch`: taken verbatim from `RepositoryBranch` when set, otherwise CI environment variables win; pull requests are recorded as `pr` plus the number. Falls back to SourceLink without CI. Only when queries are on and there is still no branch does it read `.git/HEAD` under the repository root

.NET 8+ SDK ships SourceLink built in — no `Microsoft.SourceLink.*` reference needed.

Design-time builds do not query by default (`EnableSourceControlManagerQueries` is written as `false` when unset) and all five constants are `[pending build]`. Set the property explicitly to `true` to fill them during design-time builds too.

Downstream targets can depend on `InitializeGitInformation` and read the same-named MSBuild properties: `RepositoryCommit`, `RepositorySha`, `RepositoryRoot`, `RepositoryUrl`, `RepositoryBranch`.

Branch sources: GitHub Actions, Azure DevOps, GitLab, AppVeyor, TeamCity, Travis CI, CircleCI, Jenkins and Buddy.

---

把当前仓库的 Git 信息生成为 `AssemblyMeta.Git` 常量。

- `Commit`：`RepositoryCommit` 已有值时原样采用，否则是 SourceLink 的 `SourceRevisionId`
- `Sha`：`RepositorySha` 已有值时原样采用，否则是 `Commit` 的前 9 位；不足 9 位时用全长
- `Root`：仓库根目录，正斜杠，非空时以斜杠结尾
- `Url`：仅当 `PublishRepositoryUrl=true`，或已经设置 `RepositoryUrl` 时填充
- `Branch`：`RepositoryBranch` 已有值时原样采用，否则 CI 环境变量优先；拉取请求记为 `pr` 加编号。没有 CI 时回落到 SourceLink。查询已打开且仍没有分支时，才读仓库根上的 `.git/HEAD`

.NET 8+ SDK 已内置 SourceLink，不必再引用 `Microsoft.SourceLink.*`。

设计时构建默认不查询（`EnableSourceControlManagerQueries` 为空时会被写成 `false`），五个常量都是 `[pending build]`。要在设计时也填充，把该属性显式设为 `true`。

下游 targets 可以依赖 `InitializeGitInformation`，读取同名 MSBuild 属性：`RepositoryCommit`、`RepositorySha`、`RepositoryRoot`、`RepositoryUrl`、`RepositoryBranch`。

分支来源：GitHub Actions、Azure DevOps、GitLab、AppVeyor、TeamCity、Travis CI、CircleCI、Jenkins，以及 Buddy。
