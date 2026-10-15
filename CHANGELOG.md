# 变更记录

## 1.0.0

包版本是 `1.0.0`，来源只有 `Directory.Build.props` 里的 `AssemblyMetaPackageVersion`。NuGet 上还没有这些包。

### 包

- 元包 `AssemblyMeta` 聚合 `AssemblyMeta.AssemblyInfo`、`AssemblyMeta.Constants`、`AssemblyMeta.Project`、`AssemblyMeta.Metadata`、`AssemblyMeta.Git`、`AssemblyMeta.Resources`、`AssemblyMeta.Strings`。
- 七个子包包含 `analyzers/dotnet/cs`、`tasks/netstandard2.0`、`buildTransitive`。元包只聚合这七个依赖。
- 没有包图标，没有符号包。许可是 MIT。没有赞助检查、维护费或遥测。

### 生成物

- 默认生成全局命名空间里的 `internal static partial class AssemblyMeta`。`AssemblyMetaNamespace` 可改命名空间，`AssemblyMetaVisibility` 可改为 `public`。
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
