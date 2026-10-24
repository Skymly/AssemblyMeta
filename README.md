# AssemblyMeta

中文 | [English](README.en.md)

对标 [ThisAssembly](https://clarius.org/ThisAssembly)：把项目/程序集元信息在编译期生成为强类型常量（Roslyn Source Generator），消除运行时反射样板代码。

```csharp
// 代替 Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyVersionAttribute>()...
// 类默认在 RootNamespace。顶层语句先 using 那个命名空间。
var version = AssemblyMeta.Info.Version;
var timeout = TimeSpan.FromSeconds(AssemblyMeta.Constants.Http.TimeoutSeconds);
```

- License：MIT（纯免费，无维护费模式）
- NuGet：[`AssemblyMeta`](https://www.nuget.org/packages/AssemblyMeta)（Trusted Publishing，标签触发发布）
- 仓库：[`Skymly/AssemblyMeta`](https://github.com/Skymly/AssemblyMeta)


## 当前进度

1.0.0 的行为记在 [CHANGELOG.md](CHANGELOG.md)。元包聚合 AssemblyInfo、Constants、Project、Metadata、Git、Resources、Strings。生成器是 `IIncrementalGenerator`，数据经 Base64 载荷进入 `build_property.*`，不使用 `AdditionalFiles`。文档站用 `dotnet docfx docs/docfx.json` 构建到 `artifacts/docs`。

- 架构说明：[docs/架构.md](docs/架构.md)
- 使用说明：[docs/使用说明.md](docs/使用说明.md)
- 端到端验证：`./eng/verify.ps1`

## 发布

包版本只有 `Directory.Build.props` 的 `AssemblyMetaPackageVersion`。推送标签 `v` 加这个版本。当前版本是 `1.0.0`，推 `v1.0.0`。

`.github/workflows/release.yml` 在该标签上跑 `./eng/verify.ps1`，再用 NuGet Trusted Publishing 发到 `https://api.nuget.org/v3/index.json`。先推七个子包，再推元包，然后用 `CHANGELOG.md` 对应一节建 GitHub Release。不使用长期 API key，仓库里也不放 NuGet 密钥。只有 `Skymly` 或 `wys0610` 推送的标签会发布；代理代推时认 `github.triggering_actor`。其他人推标签会让这次发布失败，而不是静默跳过。

nuget.org 策略认的是仓库 `Skymly/AssemblyMeta`、工作流文件名 `release.yml`、包主 `Skym`、包名 `AssemblyMeta*`。这个 job 不能加 GitHub Environment，策略里没填。策略创建后 7 天内要有一次成功发布，才会永久生效。

## 差异化（vs ThisAssembly）

两边都是 `IIncrementalGenerator`，都只生成 C#，都支持 file-based apps（`#:package`）。下表只列真实差别，其中一些是取舍而不是单向优势。

| 点 | ThisAssembly | AssemblyMeta |
|---|---|---|
| 许可 | 源码 MIT；2.1 起 NuGet 包附 OSMF EULA：安装时须接受，营收用户付维护费；没有技术强制，自行编译不受约束 | SPDX `MIT`；不用接受许可，不收费 |
| 增量输入 | AdditionalTexts 合并 AnalyzerConfigOptions，再合并 ParseOptions | 只有一条载荷字符串和语言版本；七个生成器都有缓存测试 |
| 换行 | 生成源码与多行常量值随构建 OS（Windows 上 CRLF，Linux 上 LF） | 固定 `\n`，不同 OS 构建结果一致 |
| Strings | 运行时按资源取值；支持卫星文化，`{0}` 与具名占位符生成带参方法 | 编译期常量；占位符留在字面量里；卫星 resx 不参与 |
| 成员形态 | `internal` 一律 `const`；`public` 是 `=>` 属性 | 非常量类型自动 `static readonly`；`public` 时统一是字段，不能写进特性参数 |
| 命名空间 | 未设置和空白都是全局 | 未设置用 `RootNamespace`；显式空白才是全局 |
| Git | SDK 9+ 本地分支前缀剥错（`feature/demo` 变成 `s/heads/feature/demo`）；提交号不足 9 位构建失败 | 分支按前缀剥离；短提交号用全长；另认 Azure DevOps 与 Jenkins 的 PR 号 |
| 文本资源 | 默认文本扩展名含 `.resx`，运行时访问 `Text` 找不到清单资源 | `.resx` 默认不进入 Resources，交给 Strings |
| 命名健壮性 | C# 关键字不转义；片段冲突抛异常，生成器崩溃 | 关键字加 `@`；冲突与改写报告 `AM0001`/`AM0008` |
| 诊断 | TA001–TA003 | `AM0001`–`AM0011`，分生成器与 MSBuild 两路 |
| 发布 | 长期 `NUGET_API_KEY` | OIDC Trusted Publishing，限定推送者 |

AssemblyMeta 的缺口：Strings 没有本地化和带参格式化方法；`AssemblyMeta.Info` 固定九个字段，上游收集全部 `Assembly*` 特性；没有 `Root`/`RootComment`、`Kind`、Vsix；文档与诊断以中文为主。

## 生成入口

- `static partial class AssemblyMeta`。未设置 `AssemblyMetaNamespace` 时用项目的 `RootNamespace`；显式设成空白时在全局命名空间
- 顶层语句需要 `using` 那个命名空间，才能直接写 `AssemblyMeta.名字`
- 各子包向该 partial 类挂嵌套类型（`Info`、`Constants`、`Git`、`Project`、`Metadata`、`Resources`、`Strings`）

## 包

| 包 | 功能 |
|---|---|
| `AssemblyMeta` | 元包，聚合下列七个子包 |
| `AssemblyMeta.AssemblyInfo` | 九个程序集信息常量 |
| `AssemblyMeta.Constants` | `<Constant>` / `<FileConstant>` MSBuild 项 → 强类型常量 |
| `AssemblyMeta.Project` | `<ProjectProperty>` opt-in 项目属性常量 |
| `AssemblyMeta.Metadata` | `<AssemblyMetadata>` 项常量 |
| `AssemblyMeta.Git` | Commit/Sha/Root/Url/Branch（SDK 内置 SourceLink） |
| `AssemblyMeta.Resources` | `EmbeddedResource` 强类型访问（Text/GetBytes/GetStream） |
| `AssemblyMeta.Strings` | 中性 resx 编译期常量 |

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

- `buildTransitive` 把 props 和 targets 交给写下 `PackageReference` 的项目。分析器停在这一层，不出现在再引用该项目的下一个项目里
- 每个生成器的增量输入只有一条载荷字符串和 C# 语言版本
- 只生成 C#。不按语言分派 emitter

## 测试策略

- 单测：Roslyn 编译断言（`Microsoft.CodeAnalysis.CSharp` 驱动 generator，校验生成语法树）
- e2e：`samples/` 样例项目走真实 `dotnet build`，断言编译产物中的常量值

## 参考

- ThisAssembly 源码：[devlooped/ThisAssembly](https://github.com/devlooped/ThisAssembly)
- 关键 MSBuild 机制：`CompilerVisibleProperty`、`CompilerVisibleItemMetadata`、`EnableSourceControlManagerQueries`
