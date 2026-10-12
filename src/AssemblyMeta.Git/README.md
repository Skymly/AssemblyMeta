# AssemblyMeta.Git

把当前仓库的 Git 信息生成为 `AssemblyMeta.Git` 常量。

- `Commit`：完整提交哈希，来自 SourceLink 的 `SourceRevisionId`
- `Sha`：`Commit` 的前 9 位
- `Root`：仓库根目录，正斜杠，并以斜杠结尾
- `Url`：仅当 `PublishRepositoryUrl=true`，或已经设置 `RepositoryUrl` 时填充
- `Branch`：CI 环境变量优先；拉取请求记为 `pr` 加编号。没有 CI 时回落到 SourceLink 分支或 `.git/HEAD`

.NET 8+ SDK 已内置 SourceLink，不必再引用 `Microsoft.SourceLink.*`。

设计时构建默认不查询（`EnableSourceControlManagerQueries` 为空时会被写成 `false`），五个常量都是 `[pending build]`。要在设计时也填充，把该属性显式设为 `true`。

下游 targets 可以依赖 `InitializeGitInformation`，读取同名 MSBuild 属性：`RepositoryCommit`、`RepositorySha`、`RepositoryRoot`、`RepositoryUrl`、`RepositoryBranch`。

分支来源：GitHub Actions、Azure DevOps、GitLab、AppVeyor、TeamCity、Travis CI、CircleCI、Jenkins，以及 Buddy。
