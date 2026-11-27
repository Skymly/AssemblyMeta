# AssemblyMeta

[English](README.md) | **简体中文** | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md)

[![Verify](https://github.com/Skymly/AssemblyMeta/actions/workflows/verify.yml/badge.svg)](https://github.com/Skymly/AssemblyMeta/actions/workflows/verify.yml)
[![NuGet](https://img.shields.io/nuget/v/AssemblyMeta?label=NuGet&logo=nuget)](https://www.nuget.org/packages/AssemblyMeta)
[![GitHub Release](https://img.shields.io/github/v/release/Skymly/AssemblyMeta?label=Release)](https://github.com/Skymly/AssemblyMeta/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

对标 [ThisAssembly](https://clarius.org/ThisAssembly)：基于 Roslyn 增量源生成器，将 MSBuild、程序集、Git、资源与字符串元信息在编译期生成为强类型 C# 和 Visual Basic 常量——MIT 许可，无运行时反射样板代码。

```csharp
// 代替 Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyVersionAttribute>()...
// 类默认在 RootNamespace。顶层语句先 using 那个命名空间。
var version = AssemblyMeta.Info.Version;
var timeout = TimeSpan.FromSeconds(AssemblyMeta.Constants.Http.TimeoutSeconds);
```

## 安装

安装元包（或按需安装下方任一子包）：

```bash
dotnet add package AssemblyMeta
```

## 概览

`AssemblyMeta` 元包聚合七个 `IIncrementalGenerator` 子包（`AssemblyInfo`、`Constants`、`Project`、`Metadata`、`Git`、`Resources`、`Strings`），同时支持 C# 与 Visual Basic。无文件锚点的数据经 Base64 载荷进入 `build_property.*`；`Strings` 的 `.resx` 文件走 `AdditionalFiles`，IDE 保存即刷新。

- 架构说明：[docs/架构.md](docs/架构.md)
- 使用说明：[docs/使用说明.md](docs/使用说明.md)
- 端到端验证：`./eng/verify.ps1`
- 变更记录：[CHANGELOG.md](CHANGELOG.md) · 文档站构建：`dotnet docfx docs/docfx.json` → `artifacts/docs`

## 差异化（vs ThisAssembly）

两边都是 `IIncrementalGenerator`。AssemblyMeta 生成 C# 和 Visual Basic；file-based apps（`#:package`）仍只有 C#。下表只列真实差别，其中一些是取舍而不是单向优势。

| 点 | ThisAssembly | AssemblyMeta |
|---|---|---|
| 许可 | 源码 MIT；2.1 起 NuGet 包附 OSMF EULA：安装时须接受，营收用户付维护费；没有技术强制，自行编译不受约束 | SPDX `MIT`；不用接受许可，不收费 |
| 增量输入 | AdditionalTexts 合并 AnalyzerConfigOptions，再合并 ParseOptions | 载荷生成器只有一条载荷字符串和语言版本；Strings 是 resx 附加文件加标量；七个生成器都有缓存测试 |
| 换行 | 生成源码与多行常量值随构建 OS（Windows 上 CRLF，Linux 上 LF） | 固定 `\n`，不同 OS 构建结果一致 |
| Strings | 运行时按资源取值；支持卫星文化，`{0}` 与具名占位符生成带参方法 | 编译期常量；占位符留在字面量里；卫星 resx 不参与 |
| 成员形态 | `internal` 一律 `const`；`public` 是 `=>` 属性 | 非常量类型自动 `static readonly`；`public` 时统一是字段，不能写进特性参数 |
| 命名空间 | 未设置和空白都是全局 | 未设置用 `RootNamespace`；显式空白才是全局 |
| Git | SDK 9+ 本地分支前缀剥错（`feature/demo` 变成 `s/heads/feature/demo`）；提交号不足 9 位构建失败 | 分支按前缀剥离；短提交号用全长；另认 Azure DevOps 与 Jenkins 的 PR 号 |
| 文本资源 | 默认文本扩展名含 `.resx`，运行时访问 `Text` 找不到清单资源 | `.resx` 默认不进入 Resources，交给 Strings |
| 命名健壮性 | C# 关键字不转义；片段冲突抛异常，生成器崩溃 | C# 关键字加 `@`；VB 关键字用方括号；VB 里只差大小写报 `AM0001`；冲突与改写报告 `AM0001`/`AM0008` |
| 诊断 | TA001–TA003 | `AM0001`–`AM0013`，分生成器与 MSBuild 两路 |
| 发布 | 长期 `NUGET_API_KEY` | OIDC Trusted Publishing，限定推送者 |

仍不做的：F#；Strings 没有本地化和带参格式化方法；`AssemblyMeta.Info` 固定九个字段，上游收集全部 `Assembly*` 特性；没有 `Root`/`RootComment`、`Kind`、Vsix、F#。文档站以中文为主，诊断是英中双语。

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
- 六个载荷生成器的增量输入只有一条载荷字符串和 C# 语言版本；Strings 的输入是 resx `AdditionalFiles` 加两个标量属性
- C# 与 Visual Basic 各有一套生成器，不按语言分派。`SourceEmission.Emit` 仍只服务 C#。VB 程序集在 `analyzers/dotnet/roslyn4.11/vb`

## 测试策略

- 单测：Roslyn 编译断言（`Microsoft.CodeAnalysis.CSharp` 驱动 generator，校验生成语法树）
- e2e：`samples/` 样例项目走真实 `dotnet build`，断言编译产物中的常量值

## 发布与参考

- 发布由标签触发，并通过 NuGet Trusted Publishing（OIDC）自动推送；详见 [docs/releasing.md](docs/releasing.md)。
- ThisAssembly 源码：[devlooped/ThisAssembly](https://github.com/devlooped/ThisAssembly)
- 关键 MSBuild 机制：`CompilerVisibleProperty`、`CompilerVisibleItemMetadata`、`EnableSourceControlManagerQueries`
