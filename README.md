# AssemblyMeta

对标 [ThisAssembly](https://clarius.org/ThisAssembly)：把项目/程序集元信息在编译期生成为强类型常量（Roslyn Source Generator），消除运行时反射样板代码。

```csharp
// 代替 Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyVersionAttribute>()...
var version = AssemblyMeta.Info.Version;
var timeout = TimeSpan.FromSeconds(AssemblyMeta.Constants.Http.TimeoutSeconds);
```

- License：MIT（纯免费，无维护费模式）
- NuGet：`AssemblyMeta`（未注册，空闲）
- 仓库：`Skymly/AssemblyMeta`（空闲）


## 当前进度

M3 已落地：元包聚合 AssemblyInfo、Constants、Project、Metadata、Git、Resources、Strings。生成器是 `IIncrementalGenerator`，数据经 Base64 载荷进入 `build_property.*`，不使用 `AdditionalFiles`。文档站用 `dotnet docfx docs/docfx.json` 构建到 `artifacts/docs`。

- 架构说明：[docs/架构.md](docs/架构.md)
- 使用说明：[docs/使用说明.md](docs/使用说明.md)
- 端到端验证：`./eng/verify.ps1`

## 差异化（vs ThisAssembly）

| 点 | ThisAssembly | AssemblyMeta |
|---|---|---|
| 许可 | MIT + OSMF 维护费（SponsorLink 检查 CI，有社区争议） | 纯 MIT |
| 语言 | 仅 C# | 仅 C# |
| 生成器 | Source Generator | `IIncrementalGenerator` + 严格缓存 |
| 构建 | — | deterministic build 友好，支持 file-based apps（`#:package`） |

## 生成入口

- `static partial class AssemblyMeta`，默认全局命名空间
- 命名空间可用 MSBuild 属性覆盖：`$(AssemblyMetaNamespace)`
- 各子包向该 partial 类挂嵌套类型（`Info`、`Constants`、`Git`、`Project`、`Metadata`、`Resources`、`Strings`）

## 包规划

| 包 | 功能 | 优先级 |
|---|---|---|
| `AssemblyMeta` | meta 包，汇总全部子包 | P0 |
| `AssemblyMeta.AssemblyInfo` | AssemblyVersion/InformationalVersion/Title 等特性常量 | P0 |
| `AssemblyMeta.Constants` | `<Constant>` MSBuild 项 → 强类型常量（含 `FileConstant` 文件常量） | P0 |
| `AssemblyMeta.Project` | `<ProjectProperty>` opt-in 项目属性常量 | M2 |
| `AssemblyMeta.Metadata` | `<AssemblyMetadata>` 项常量 | M2 |
| `AssemblyMeta.Git` | Commit/Sha/Branch/Url（依赖 SourceLink） | M2 |
| `AssemblyMeta.Resources` | `EmbeddedResource` 强类型访问（Text/GetBytes/GetStream） | M3 |
| `AssemblyMeta.Strings` | resx 资源常量 | M3 |

## 技术架构

每个子包两个项目：

- `AssemblyMeta.X.csproj` — packaging：打 analyzer dll + `buildTransitive` 下的 props/targets
- `AssemblyMeta.X.Generator.csproj` — netstandard2.0 analyzer，引用 `Microsoft.CodeAnalysis.CSharp`

数据链路：

```
MSBuild props/targets
  → CompilerVisibleProperty / CompilerVisibleItemMetadata
  → AnalyzerConfigOptions（build_property.* / build_metadata.*）
  → IncrementalGenerator 读取
  → emit C# 源码（AssemblyMeta partial class）
```

要点：

- `buildTransitive` 而非 `build`，保证传递引用生效
- generator 只依赖 `build_metadata.*` / `build_property.*` 输入，保证增量缓存命中率
- 只生成 C#。不按语言分派 emitter

## 里程碑

- **M1** 骨架端到端：meta 包 + AssemblyInfo + Constants，`dotnet pack` → 真实 `dotnet build` 验证生成代码
- **M2** Project + Metadata + Git（已落地）
- **M3** Resources + Strings + 文档站（已落地）

## 测试策略

- 单测：Roslyn 编译断言（`Microsoft.CodeAnalysis.CSharp` 驱动 generator，校验生成语法树）
- e2e：`samples/` 样例项目走真实 `dotnet build`，断言编译产物中的常量值

## 参考

- ThisAssembly 源码：[devlooped/ThisAssembly](https://github.com/devlooped/ThisAssembly)
- 关键 MSBuild 机制：`CompilerVisibleProperty`、`CompilerVisibleItemMetadata`、`EnableSourceControlManagerQueries`
