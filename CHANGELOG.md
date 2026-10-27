# 变更记录

## 未发布

- `AssemblyMeta.Strings` 的 resx 改经 `AdditionalFiles` 由生成器直读：IDE 里保存 resx 后生成物即时刷新。`EncodeStrings` 任务与 `AssemblyMeta_StringsPayload` 移除，Strings 包不再带 `tasks/`。
- `AM0007`、`AM0010`、`AM0011` 改由生成器报告并带文件位置；`AM0011` 收窄为无法读取或解析。新增 `AM0012`：无法识别的 `AssemblyMetaVisibility`（仅 Strings）。生成物成员不变。

## 1.0.0-preview

预发布。八个 `1.0.0-preview` 包已于 2026-10-23 发布到 nuget.org。稳定版 `1.0.0` 已于 2026-10-24 发布。

### 包

- 元包 `AssemblyMeta` 聚合 `AssemblyMeta.AssemblyInfo`、`AssemblyMeta.Constants`、`AssemblyMeta.Project`、`AssemblyMeta.Metadata`、`AssemblyMeta.Git`、`AssemblyMeta.Resources`、`AssemblyMeta.Strings`。
- 七个子包包含 `analyzers/dotnet/roslyn4.11/cs`、`tasks/netstandard2.0`、`buildTransitive`。元包只聚合这七个依赖。
- 没有包图标，没有符号包。许可是 MIT。没有赞助检查、维护费或遥测。

### 生成物

- 未设置 `AssemblyMetaNamespace` 时，根类放在 `$(RootNamespace)`。显式设成空白时仍在全局命名空间。`AssemblyMetaVisibility` 可改为 `public`。默认可见性是 `internal`。
- `internal` 时，合格类型是 `const`，其它类型是 `static readonly`。`public` 时这些成员改为 `static readonly` 字段。
- 只生成 C#。
- `AssemblyMeta.Info` 固定九个字符串：`Configuration`、`Company`、`Title`、`Description`、`Product`、`Copyright`、`Version`、`InformationalVersion`、`FileVersion`。生成器不把空字符串改成程序集名。不生成商标。`GenerateAssemblyInfo=false` 时这九个成员仍会生成。
- `Constant` 与 `FileConstant` 挂在 `AssemblyMeta.Constants`。点号变成嵌套类型。
- 项目属性挂在 `AssemblyMeta.Project`。默认含目标框架标识的标识符、Moniker 和版本。目标框架短名要另外点名 `TargetFramework`。
- `AssemblyMetadata` 项挂在 `AssemblyMeta.Metadata`。
- `AssemblyMeta.Git` 有 `Commit`、`Sha`、`Root`、`Url`、`Branch`。查询关闭时，五个生成值是 `[pending build]`。写回 `RepositoryCommit`、`RepositorySha`、`RepositoryRoot`、`RepositoryUrl`、`RepositoryBranch` 的仍是解析结果，不把占位符写进去。
- 嵌入资源挂在 `AssemblyMeta.Resources`，提供 `Text`、`GetBytes`、`GetStream`。
- 中性 resx 挂在 `AssemblyMeta.Strings`，是编译期常量。占位符留在字面量里。卫星 resx 不参与。

### 诊断

`AM0001` 到 `AM0011`。含义见 `docs/契约.md`。

### 编译器

调用方需要能提供增量生成器的编译器。本仓库写下的编译器 API 下限是 4.11.0。SDK 10.0.303 是验证环境。编译期不另做版本检查。

## 1.0.0

首个稳定版，已于 2026-10-24 发布到 nuget.org。包版本来自 `Directory.Build.props` 的 `AssemblyMetaPackageVersion`。元包 `AssemblyMeta` 聚合 `AssemblyMeta.AssemblyInfo`、`AssemblyMeta.Constants`、`AssemblyMeta.Project`、`AssemblyMeta.Metadata`、`AssemblyMeta.Git`、`AssemblyMeta.Resources`、`AssemblyMeta.Strings` 七个子包。

- 行为与 `1.0.0-preview` 相同：生成器在 `analyzers/dotnet/roslyn4.11/cs`，任务在 `tasks/netstandard2.0`，props/targets 在 `buildTransitive`。
- 包内 DLL 版本与包版本一致，携带嵌入式 PDB 与 SourceLink。
- 许可 MIT。没有包图标，没有符号包。没有赞助检查、维护费或遥测。
- 诊断 AM0001–AM0011 为英中双语，仓库提供中英文 README。
