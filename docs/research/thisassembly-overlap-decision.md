# ThisAssembly 重叠度与 AssemblyMeta 去留决策备忘录

调查日期：2026-10-24。GitHub 与 NuGet 数据快照：2026-10-24T07:43Z 起。

## 基线与引用约定

| 对象 | 版本 |
|---|---|
| 上游 devlooped/ThisAssembly | HEAD `0d5ebf1bb582dc064dd01d02baaf2659de20004d`，author 2026-10-07 11:36 +0000，committer 2026-10-08 08:31 -0300。`git describe` 为 `v2.1.6-3`，比 v2.1.6 多三个基础设施提交（changelog、dotnet-file 同步、依赖升级），生成器与 targets 与 v2.1.6 相同。NuGet 最新 2.1.6（2026-09-28）。 |
| 本仓 Skymly/AssemblyMeta | HEAD `787e764b1bd51be929c6d33ccf28e2f902d02c36`（2026-10-23 19:37 +0800）。NuGet 8 个包 1.0.0-preview 已于 2026-10-23 发布，nuspec 的 repository commit 与 HEAD 相同。 |

- `TA:路径:行` 指 `https://github.com/devlooped/ThisAssembly/blob/0d5ebf1bb582dc064dd01d02baaf2659de20004d/路径#L行`。
- `TA@v2.0.14:路径:行` 指 `https://github.com/devlooped/ThisAssembly/blob/v2.0.14/路径#L行`。提交写成 `TA@<sha>`，指 `https://github.com/devlooped/ThisAssembly/commit/<sha>`。
- `TA#n` 指 `https://github.com/devlooped/ThisAssembly/issues/n`（PR 同址跳转）。`moq#n`、`SponsorLink#n` 同理，仓库为 devlooped/moq、devlooped/SponsorLink。`AM#n` 指 `https://github.com/Skymly/AssemblyMeta/issues/n`。
- `AM:路径:行` 指本仓 HEAD `787e764` 的工作区。
- 已有调研稿 [thisassembly-parity.md](thisassembly-parity.md) 锁定 v2.1.6 标签，只写调用方可观察行为。本文不改它。它的「未核实清单」第 2 条（赞助诊断码）由本文 §5.6 回答。
- 工作底稿在仓外，未提交：`C:\Code\Skymly\ref\_notes\thisassembly-factsheet.md`、`assemblymeta-factsheet.md`（按包、带行号的源码事实清单），`thisassembly_eco.py` 与 `thisassembly-eco\raw.json`、`summary.txt`（生态数据脚本与原始结果），`%TEMP%\am-research\`（17 个 nupkg 及解包目录、Git 分支复现工程）。

## 0. 结论摘要

1. **差异化表一半站不住。** 七行里两行事实错误：ThisAssembly 现在没有 SponsorLink；它也支持 file-based apps。一行误导：ThisAssembly 从 v1.1.0 起就是 `IIncrementalGenerator`。一行不构成差异：两边都只生成 C#。剩下三行（Strings、public 成员形态、命名空间默认）属实，但都是取舍，不是单向优势。站得住而且能长期存在的差异只有三类。一是许可：SPDX `MIT`，没有 EULA，不用接受许可，不收维护费。二是边界正确性：Git 分支、短提交号、resx 当文本、关键字、命名冲突。三是工程纪律：增量输入只有字符串和整数，有缓存测试，有真实打包后的端到端验证，用 Trusted Publishing 发布。表里没写的劣势：没有本地化和带参格式化方法；AssemblyInfo 固定九个字段；没有 Vsix、Root、Kind；文档只有中文；MSBuild 任务 DLL 不随版本递增；analyzer 没放进 roslyn 版本目录；CI 只跑 Windows；没有任何外部采用。
2. **建议停止功能扩张，但不归档。** 先完成 §8 的 P0 收尾，发布 1.0.0，之后进入低维护模式。新增精力优先放到上游，只修与设计取向无关的 bug。AssemblyMeta 的设计分叉贡献不回去：它们和上游写明的选择相反，改了会破坏现有用户。外部贡献按历史数据可行：已有结论（合并或关闭）的外部 PR 中 11/14 合并，中位合并延迟 7.8 天。但需要签 CLA，维护者是单人、成批处理，最近 27 个月没有合并过外部 PR。3 到 6 个月后看外部采用信号，再决定是否归档。
3. **继续开发时应吸收的：** analyzer 放进 `roslyn4.11` 目录、DLL 版本随包、三平台 CI 矩阵与格式检查、自身包的嵌入式 PDB 与 SourceLink、英文 README，以及 Project 先跑 `InitializeSourceControlInformation`。**明确不吸收的：** OSMF 或任何赞助检查、运行时 ResourceManager 版 Strings、全局命名空间默认、Scriban 模板、Vsix、Constant 的 Root 改挂、Prerequisites。
4. **停止开发时值得送回上游的 PR：** 分支前缀剥离 bug（已复现，`TA#526` 待合）、短提交号截断、Azure DevOps 与 Jenkins 的 PR 号、TA002 文案、关键字转义、命名冲突改为诊断、默认文本扩展名不含 `.resx`、换行统一成 `\n`、SponsorLink 死代码。值得单独成文的有两篇：单属性 Base64 载荷加严格增量输入的设计说明；把 parity 稿整理成上游边界行为参考。

## 1. 事实基线

### 1.1 两边各是什么

- ThisAssembly：2020-09-15 建库。真正的生成器只有三个：Constants、Resources、Strings。AssemblyInfo、Git、Metadata、Project、Vsix 只有 targets，它们把数据转成 `@(Constant)` 项，交给 Constants 的生成器出码（`TA:src/ThisAssembly.AssemblyInfo/ThisAssembly.AssemblyInfo.targets:41-53`、`TA:src/ThisAssembly.Git/ThisAssembly.Git.targets:94-114`、`TA:src/ThisAssembly.Project/ThisAssembly.Project.targets:24-34`、`TA:src/ThisAssembly.Metadata/ThisAssembly.Metadata.targets:9-15`）。模板用 Scriban，以源码形式编进生成器（`TA:src/Directory.props:21`）。
- AssemblyMeta：七个包各带一个生成器，共用一段注册代码（`AM:src/AssemblyMeta.Generators/GeneratorHost.cs:15-48`）。MSBuild 任务把数据编码成单个 Base64 属性（`AM:src/AssemblyMeta.Constants/buildTransitive/AssemblyMeta.Constants.targets:15-25`）。emitter 是手写的 `StringBuilder`（`AM:src/AssemblyMeta.Core/Emit/CSharpSourceEmitter.cs`）。

### 1.2 生态数据

方法：`gh api` 调 GitHub REST，即 `repos/{o}/{r}`、`commits?since=`、`contributors`、`pulls?state=all`、`issues?state=all`、`issues/{n}`、`issues/{n}/comments`、`traffic/*`。NuGet 用两个接口：下载量来自 `https://azuresearch-usnc.nuget.org/query?q=packageid:{id}&prerelease=true&semVerLevel=2.0`，发布时间与许可来自 `https://api.nuget.org/v3/registration5-gz-semver2/{id}/index.json`。脚本与原始 JSON 见工作底稿。账号分三类。机器人包括 `type=Bot`、`*[bot]` 和 `devlooped-bot`。维护者是 `kzu`，以及 `author_association` 为 OWNER、MEMBER、COLLABORATOR 的账号。其余算外部。每个 PR 的状态都按 API 的 `state` 与 `merged_at` 逐个判定，没有推断。外部 PR 中已关闭未合并的，另外查了 `closed_by`。

#### ThisAssembly 仓库

| 指标 | 值 |
|---|---|
| star / fork / watcher | 532 / 24 / 5 |
| `open_issues_count` | 3。实际是 1 个 issue 加 2 个 PR。那个 issue 是维护者自己的 OSMF 公告 `TA#480`。两个 PR：`TA#525` 是维护者的文件同步，`TA#526` 是外部修复。 |
| 近 6 个月（2026-04-24 起）main 提交 | 25 个。dependabot 11、devlooped-bot 7、kzu 6、Copilot 1。只分布在 6 天里：06-22、07-09、08-27（11 个，当天连发 v2.1.3、v2.1.4、v2.1.5）、09-28（v2.1.6）、10-06、10-08。 |
| 近 12 / 24 个月提交 | 27 / 101。2025-11 到 2026-03 连续五个月没有提交。 |
| 发布 | 2.1.0 到 2.1.2 在 2025-10-16、17 两天发出。之后十个月没有发布，直到 2026-08-27 一天连发三版，2026-09-28 发 2.1.6。 |
| 历史贡献者 | kzu 316，devlooped-bot 170，dependabot 96，viceroypenguin 12，另有 5 人各 1 次。kzu 占人类提交的 95%（316/333）。 |
| PR 总数 | 454 个。维护者 144 个：合并 140、关闭 3、打开 1。机器人 295 个：合并 189、关闭 106。外部 15 个：合并 11、关闭 3、打开 1。 |
| 外部 PR 合并率 | 已决的 14 个中合并 11 个，78.6%。去掉作者自撤的 `TA#133`，是 11/13，84.6%。 |
| 外部 PR 合并延迟 | 中位 7.8 天，最长 39.6 天。维护者自己的 PR 中位 0 天。 |
| 近 24 个月外部 PR | 只有 1 个：`TA#526`，2026-10-16 提交，至今未合并，CLA 未签，维护者没有回复。最后一次合并外部 PR 是 `TA#362`，2024-07-26 提交。 |
| issue（不含 PR） | 历史 63 个。近 24 个月 6 个，其中外部 4 个：`TA#442` 3.5 天首次回复，已修；`TA#467` 维护者没回复，被 stale 机器人以 not_planned 关闭，但 changelog v2.1.3 把它列为已修；`TA#493` 127.5 天后首次回复，以 not_planned 关闭，由 TA003（`TA#509`）处理；`TA#506` 没有评论，直接由维护者以 completed 关闭（`TA#508` 修复）。 |
| stale 策略 | 180 天贴 stale，再过 30 天关闭，kzu 本人豁免（`TA:.github/workflows/triage.yml:62-80`）。 |

#### NuGet 下载

累计下载与最新版下载（2.1.6，发布已 26.2 天）：

| 包 | 累计 | 2.1.6 下载（每天） | 2.0.14 下载（最后一个 MIT 版） | 2.1.2 下载（第一个 EULA 稳定版之后） |
|---|---|---|---|---|
| ThisAssembly | 273,751 | 1,652（63） | 21,944 | 18,293 |
| ThisAssembly.AssemblyInfo | 405,498 | 2,952（113） | 39,590 | 46,147 |
| ThisAssembly.Constants | 4,062,325 | 4,923（188） | 55,310 | 781,440 |
| ThisAssembly.Git | 244,375 | 2,328（89） | 37,939 | 39,302 |
| ThisAssembly.Metadata | 241,699 | 1,757（67） | 27,386 | 24,616 |
| ThisAssembly.Project | 327,047 | 2,750（105） | 37,019 | 49,178 |
| ThisAssembly.Resources | 251,330 | 1,715（65） | 38,202 | 32,102 |
| ThisAssembly.Strings | 227,359 | 1,703（65） | 27,433 | 21,870 |
| ThisAssembly.Vsix | 11,758 | 134（5） | 711 | 618 |
| AssemblyMeta 全部 8 个包 | 各 0 | 1.0.0-preview 发布才 0.8 天 | — | — |

读法：

- Constants 的累计数大部分来自依赖：nuget.org 显示 GitInfo（1290 万下载）依赖它，「Used By」共 7 个 NuGet 包，热门 GitHub 仓库只有 1 个（`https://www.nuget.org/packages/ThisAssembly.Constants`）。它不能当作 ThisAssembly 自身的用户数。
- **爬虫基线。** 2.1.3 和 2.1.4 发布当天就被 2.1.5 取代，各包仍各有 150 到 730 次下载。Vsix 的 2.1.x 每个版本 130 到 620 次。本人 owner `Skym` 名下其它包大多每版本 80 到 150 次，R3 和 Reactive 两个变体的数字几乎一样。所以每个版本、每个包有一两百次下载，应视为镜像和扫描流量，不是用户。AssemblyMeta 的元包依赖七个子包，装一次元包，八个包各加一次。
- 下载量里还有 CI 每次无缓存的还原，不能当成独立用户。GitHub dependency graph 显示依赖 ThisAssembly 元包的公开仓库约 84 个、包 6 个，这个指标比下载量更接近采用度（`https://github.com/devlooped/ThisAssembly/network/dependents`，GitHub 注明是近似值）。
- 2.1.x 改用 OSMF EULA 之后，各包 2.1.2 的下载与 2.0.14 同量级，看不出用户流失。2.0.x 的许可字段是 `MIT` 表达式，2.1.x 是许可文件 URL（registration 的 catalogEntry）。

#### AssemblyMeta 采用证据

- 0 star、0 fork、0 watcher。36 个 issue 全由 Skymly 在 2026-10-14、15 两天创建，是规划和决策用的。0 个 PR。
- 14 天流量：页面浏览 1 次（1 人）。clone 615 次（272 个来源），其中 2026-10-16 一天 369 次，没有 referrer。clone 主要是 CI 和自动化。
- NuGet 下载 0。发布不足 1 天，统计有延迟，所以 0 不代表没人装。但现在也没有任何采用证据。注意：本次调研从 flat container 把 8 个 AssemblyMeta 包各下载了 1 次，复现时又从 nuget.org 还原过 1 次 AssemblyMeta.Git。这些都会进入以后的计数。

## 2. README 差异化表逐条核验

`AM:README.md:33-43`：

| 行 | README 写法 | 核验 | 证据 | 建议改写 |
|---|---|---|---|---|
| 许可 | ThisAssembly：MIT + OSMF 维护费（SponsorLink 检查 CI，有社区争议） | **部分错误。** 维护费属实，但 SponsorLink 已删除。v2 时期它也只在 VS 或 Rider 里生效，从不在 CI 检查；v1 是否跳过 CI 未核实。源码许可是 MIT。v2.1.0（2025-10-16）起，NuGet 包许可换成 OSMF EULA 文件，并要求接受许可。营收用户须付费，没有技术强制。争议属实，但都是 2023 到 2024 年的事。 | `TA:license.txt:1-3`；`TA:src/Directory.props:17-18,38`；`TA:osmfeula.txt:12-22,42-49`；`TA@7ab64af`；`TA@v2.0.14:src/SponsorLink/SponsorLink/SponsorLink.cs:37-51`；`TA#352`；`moq#1372`；见 §5.6 | ThisAssembly：源码 MIT；2.1 起 NuGet 包附 OSMF EULA（安装时须接受，营收用户付维护费）。AssemblyMeta：SPDX `MIT`，不用接受许可，不收费。 |
| 语言 | 两边都只生成 C# | 属实，但不构成差异 | `TA:src/ThisAssembly.Constants/ConstantsGenerator.cs:15`；`AM:docs/adr/0002-no-language-seam-for-source-emission.md:3` | 从差异表删掉 |
| 生成器 | ThisAssembly 是 Source Generator；AssemblyMeta 是 `IIncrementalGenerator` 加严格缓存 | **误导。** ThisAssembly 三个生成器都是 `IIncrementalGenerator`，从 v1.1.0（`TA#132`）开始。真正的差别在增量输入的形态和有没有缓存测试，见 §5.1。没有测过两边的性能。 | `TA:src/ThisAssembly.Constants/ConstantsGenerator.cs:15-16`；`TA:changelog.md:645`；`AM:src/AssemblyMeta.Generators/GeneratorHost.cs:21-29` | 两边都是增量生成器。AssemblyMeta 的增量输入只有一条载荷字符串和语言版本，七个生成器都有缓存测试。 |
| 构建 | ThisAssembly 写「—」；AssemblyMeta 写 deterministic build 友好、支持 file-based apps | **部分错误。** ThisAssembly 文档写明了 file-based apps 用法。确定性方面两边在同一平台上都确定；能成立的差别只有跨 OS：ThisAssembly 按 `Environment.NewLine` 和当前 OS 决定换行，多行常量的值在 Windows 上是 CRLF，在 Linux 上是 LF。AssemblyMeta 固定用 `\n`，但 CI 只跑 Windows，没有验证过这一点。 | `TA:readme.md:274-278`；`TA:src/ThisAssembly.Project/ThisAssembly.Project.targets:26-27`；`TA:src/ThisAssembly.Constants/ConstantsGenerator.cs:93-98,114,129-134`；`AM:src/AssemblyMeta.Core/Emit/CSharpLiteral.cs:73`；`AM:.github/workflows/verify.yml:14` | 生成源码和多行常量值固定用 `\n`，不同 OS 构建结果一致。ThisAssembly 随构建 OS 变化。（补上三平台 CI 以后再写） |
| Strings | ThisAssembly 运行时按资源取值；AssemblyMeta 编译期常量，占位符留在字面量里，卫星 resx 不参与 | 属实。但这是取舍：ThisAssembly 因此支持本地化，`{0}` 和具名占位符会生成带参方法。 | `TA:src/ThisAssembly.Strings/CSharp.sbntxt:27-58`；`TA:src/ThisAssembly.Strings/ThisAssembly.Strings.sbntxt:26-32`；`AM:docs/adr/0003-compile-time-strings-and-static-readonly-fields.md:3` | 保留，并补一句：不支持卫星文化和带参方法 |
| public 成员 | ThisAssembly 是 get-only 属性；AssemblyMeta 是 `static readonly` 字段 | 属实。两边都是为了不把值内联进引用方，效果相同，差别只在形态。这一行之外的一个真实差别：ThisAssembly 在 internal 下一律写 `const`，`Type` 填 `System.TimeSpan` 就编译不过；AssemblyMeta 只对合格类型用 `const`。 | `TA:src/ThisAssembly.Constants/Model.cs:17-19`；`TA:src/visibility.md:9-10,26-38`；`AM:src/AssemblyMeta.Core/Emit/CSharpSourceEmitter.cs:340-357`；`AM:src/AssemblyMeta.Core/IdentifierSanitizer.cs:152` | 改成「非常量类型自动用 `static readonly`；public 时是字段」 |
| 命名空间 | ThisAssembly 未设置和空白都是全局；AssemblyMeta 未设置用 `RootNamespace`，显式空白才是全局 | 属实。这是取舍：放进 RootNamespace 减少了多程序集同名全局类型冲突（参照 `TA#442`），代价是顶层语句要写 `using`。 | `TA:src/ThisAssembly.Constants/ConstantsGenerator.cs:66`；`TA:src/ThisAssembly.Constants/CSharp.sbntxt:51-53`；`AM:src/build/AssemblyMeta.Namespace.targets:11-16` | 保留 |

## 3. 表里没写的差异

### 3.1 AssemblyMeta 有证据的优势

1. **Git 分支正确。** 上游先拿 SourceRoot 的 `BranchName`，然后用 `TrimStart("ref:")` 去前缀（`TA:src/ThisAssembly.Git/ThisAssembly.Git.targets:76,89`）。MSBuild 把它当成字符集合来裁剪。同一个表达式实测：`refs/heads/main` 变成 `s/heads/main`；`.git/HEAD` 的文本 `ref: refs/heads/main` 正常得到 `main`。端到端复现环境是 SDK 10.0.303、本地仓库分支 `feature/demo`、没有 CI 变量：ThisAssembly.Git 2.1.6 生成的 `Branch` 是 `s/heads/feature/demo`，AssemblyMeta.Git 1.0.0-preview 是 `feature/demo`。AssemblyMeta 用 `StartsWith` 加 `Substring` 处理（`AM:src/AssemblyMeta.Core/GitInformation.cs:217-243`）。上游有外部修复 `TA#526`，还没合并。
2. **短提交号。** 上游用 `Substring(0, 9)`（`TA:src/ThisAssembly.Git/ThisAssembly.Git.targets:58`），提交号短于 9 位时构建失败，报 MSB4184（复现见 `AM#26`）。AssemblyMeta 不足 9 位时用全长（`AM:src/AssemblyMeta.Core/GitInformation.cs:195-204`）。
3. **CI 的 PR 号。** 上游对 Azure DevOps 只读 `BUILD_SOURCEBRANCH`，对 Jenkins 只读 `GIT_LOCAL_BRANCH`（`TA:src/ThisAssembly.Git/ThisAssembly.Git.targets:18,42`）。AssemblyMeta 还读 `SYSTEM_PULLREQUEST_PULLREQUESTID` 和 `CHANGE_ID`（`AM:src/AssemblyMeta.Core/GitInformation.cs:86-109`）。
4. **resx 不当文本。** 上游默认文本扩展名包含 `.resx`（`TA:src/ThisAssembly.Resources/ThisAssembly.Resources.props:4`），访问 `Text` 时运行时找不到清单资源（复现见 `AM#25`）。AssemblyMeta 默认排除 resx。
5. **命名健壮性。** 上游不转义 C# 关键字（`TA:src/ThisAssembly.Constants/Model.cs:39-58`），分段冲突时直接抛 `ArgumentException`，生成器崩溃（`:103-106`）。AssemblyMeta 给关键字加 `@`，冲突报 AM0001 或 AM0008。
6. **诊断体系。** AssemblyMeta 有 AM0001 到 AM0011，分生成器和 MSBuild 两路报告。上游只有 TA001（MSBuild）、TA002、TA003。其中 TA002 的文案写「需要 C# 8.0」，实际判断的是 C# 10（`TA:src/ThisAssembly.Constants/ConstantsGenerator.cs:83-87`）。
7. **供应链。** AssemblyMeta 用 OIDC Trusted Publishing，并限定推送者（`AM:.github/workflows/release.yml:25,111`）。上游用长期密钥 `NUGET_API_KEY`（`TA:.github/workflows/publish.yml:3,57-62`）。
8. **验证深度。** AssemblyMeta 七个生成器都用 `trackIncrementalGeneratorSteps` 断言缓存（`AM:tests/AssemblyMeta.Generator.Tests/GeneratorTestHost.cs:142-192`）。包布局测试真实调用 `dotnet pack`，`verify.ps1` 用打好的包构建并运行样例，包括 file-based app（`AM:eng/verify.ps1:35-103`）。上游的测试是把生成器以项目引用接进测试工程，生成物编进测试程序集直接断言（`TA:src/ThisAssembly.Tests/ThisAssembly.Tests.csproj:40-45`），外加 `GitTests` 起一个 `dotnet msbuild` 子进程（`TA:src/ThisAssembly.Tests/GitTests.cs:76-137`）。全仓搜不到 `TrackIncrementalGeneratorSteps`，没有缓存断言，也没有用打好的包测试。
9. **契约与 SemVer 政策。** `AM:docs/契约.md:111-115` 写明了哪些改动算破坏性变更。上游没有对应文档。

### 3.2 AssemblyMeta 的劣势与风险

1. **采用为零**（§1.2）。上游有约 84 个依赖仓库，最新版各包每天几十到近两百次下载，还被 GitInfo 依赖。
2. **功能缺口。**
   - Strings 不支持本地化，也没有带参格式化方法。
   - AssemblyInfo 固定九个字段。上游会收集全部 `System.Reflection.Assembly*` 特性（`TA:src/ThisAssembly.AssemblyInfo/ThisAssembly.AssemblyInfo.targets:44-52`）。
   - Resources 没有扩展名列表、`Kind`，也不区分文本和二进制。
   - 没有 Constant 的 `Root` 和 `RootComment`，没有 Vsix。
3. **只有中文。** README、NuGet 包描述、诊断文案都是中文，比如 AM0004 的文案（`AM:src/AssemblyMeta.Core/SourceEmission.cs:77-78`）。这会挡住非中文用户。
4. **任务 DLL 的版本与重复。** nupkg 实测：七个子包各带一份 `AssemblyMeta.Tasks.dll` 和两份 `AssemblyMeta.Core.dll`（analyzers 和 tasks 下各一份）。所有 DLL 的 AssemblyVersion 都是 1.0.0.0，InformationalVersion 是 `1.0.0+787e764`，不是 `1.0.0-preview`。Tasks、Core、Generator 三个项目都没有设置版本（`AM:src/AssemblyMeta.Tasks/AssemblyMeta.Tasks.csproj:1-12`）。同一版本内这些副本内容相同，不会冲突。风险在跨版本：.NET Framework 版 MSBuild（VS、msbuild.exe）用 `Assembly.LoadFrom` 加载时，同一程序集标识已加载就直接复用（[LoadFrom 文档](https://learn.microsoft.com/dotnet/api/system.reflection.assembly.loadfrom)）。同一个 MSBuild 节点里出现两个 AssemblyMeta 版本时，可能执行到旧任务代码。这一条是推断，没有实测。上游没有编译任务，DLL 版本随包，是 2.1.6.0。
5. **analyzer 路径。** AssemblyMeta 引用 Microsoft.CodeAnalysis.CSharp 4.11.0（`AM:src/AssemblyMeta.Constants.Generator/AssemblyMeta.Constants.Generator.csproj:11`），却打进 `analyzers/dotnet/cs`（`AM:src/AssemblyMeta.Packaging.targets:61-62`）。编译器低于 4.11 时会报 CS9057，生成器不运行。上游放在 `roslyn4.0` 目录（`TA:src/Directory.props:9-10`）。SDK 会选不高于当前编译器版本的最高目录，没有合适目录就不加载（[dotnet/sdk#20793](https://github.com/dotnet/sdk/pull/20793)）。
6. **只在 Windows 上跑 CI**（`AM:.github/workflows/verify.yml:14`）。上游是 ubuntu、macOS、windows 三平台，外加 `dotnet format`（`TA:.github/workflows/os-matrix.json:1`、`TA:.github/workflows/build.yml:93-108`）。
7. **IDE 中 Strings 可能滞后。** AssemblyMeta 在 MSBuild 任务里读 resx（`AM:src/AssemblyMeta.Tasks/EncodeStrings.cs:49-59`），生成器只看载荷。上游把 resx 作为 AdditionalFiles 交给生成器读文本（`TA:src/ThisAssembly.Strings/StringsGenerator.cs:38-49`）。推断：只改 resx 内容、没触发设计时构建时，AssemblyMeta 的 Strings 会滞后，上游不会。没在 VS 或 Rider 里实测。
8. **Project 的编排缺口。** 上游先跑 `InitializeSourceControlInformation` 再求值属性（`TA:src/ThisAssembly.Project/ThisAssembly.Project.targets:18-21`），AssemblyMeta 没有（`AM:src/AssemblyMeta.Project/buildTransitive/AssemblyMeta.Project.targets:15-17`）。只引用 Project 包、又点名 `SourceRevisionId` 之类属性时，可能取到空值。没有实测。
9. **自身包没有符号和 SourceLink。** 上游自身包有嵌入式 PDB 和 SourceLink（`TA:src/Directory.Build.props:64,78-81`）。

## 4. 按子包对齐

| 子包 | ThisAssembly（HEAD） | AssemblyMeta（HEAD） | 判断 |
|---|---|---|---|
| AssemblyInfo | 没有自己的生成器。收集 `System.Reflection.Assembly*` 特性（AssemblyMetadata 除外），成员名去掉前缀和后缀；`GenerateAssemblyInfo=false` 时强制打开各个 `Generate*Attribute`（`TA:…AssemblyInfo.targets:13-53`）。 | 固定九个 string，空值写空串；`GenerateAssemblyInfo=false` 时照样生成（`AM:docs/契约.md:24`）。 | ThisAssembly 覆盖面更广；AssemblyMeta 契约更稳。属于取舍。 |
| Constants | `Constant` 有 Value、Type、Comment、Root、RootComment，另有 `FileConstant`。默认 `const`，public 时是 `=>` 属性。每个常量一个文件，按 OS 决定换行（`TA:…ConstantsGenerator.cs:20-119`）。 | `Constant` 有 Value、Type、Comment；`FileConstant` 支持 Link，路径用正斜杠。const 只限合格类型。关键字加 `@`，冲突报诊断，所有成员在一个文件里。 | AssemblyMeta 更健壮；ThisAssembly 多了 Root。 |
| Project | 七个默认属性，外加 `ProjectProperty` 项或属性（支持 `#:property`），先跑 `InitializeSourceControlInformation`（`TA:…Project.props:4-12`、`…Project.targets:18-32`）。 | 默认属性和入口相同，但没有 SourceLink 前置。 | 基本对齐，AssemblyMeta 有一个小缺口（§3.2 第 8 条）。 |
| Metadata | `@(AssemblyMetadata)` 生成 `Metadata.*`（`TA:…Metadata.targets:13`）。 | 相同，只读不改写。 | 对齐。 |
| Git | 五个成员，覆盖九家 CI。`Substring(0,9)`；SDK 9 以上本地分支错误；`[pending build]`；TA001。 | 五个成员，21 个 CI 键。短提交号用全长，分支正确。设计时主动关闭查询。占位符不写回 `Repository*`（ADR 0001）。AM0006。 | AssemblyMeta 明显更正确。 |
| Resources | 所有 EmbeddedResource 都纳入，由扩展名列表或 `Kind` 决定有没有 Text，支持 Comment。按 `EndsWith` 匹配清单名。默认扩展名包含 `.resx`（`TA:…Resources.props:4`、`…Resources.targets:47-48`）。 | 固定三件套：Text、GetBytes、GetStream。按清单名精确打开，默认排除 `.resx`，有 `AssemblyMetaResource` 开关。 | ThisAssembly 能区分文本和二进制；AssemblyMeta 正确性更好。 |
| Strings | 中性 resx 运行时用 ResourceManager 取值，支持卫星文化。占位符生成带参方法。有 TA003。resx 经 AdditionalFiles 传入。 | 编译期常量，占位符留在字面量里，卫星 resx 不参与。在 MSBuild 任务里读 resx，有 AM0007 和 AM0011。 | 设计分叉：ThisAssembly 面向本地化，AssemblyMeta 面向常量用法。 |
| Vsix | 有：VS SDK 清单和常量互通，并改写 Pack（`TA:src/ThisAssembly.Vsix/ThisAssembly.Vsix.targets:22-122`）。 | 没有（非目标，`AM#17`）。 | 各版本下载 130 到 620 次，接近基线，需求很小。 |
| Prerequisites | 停在 1.2.11（2023-03）。HEAD 上的 `src/ThisAssembly.Prerequisites.targets` 没人引用，也不进包。 | 没有（非目标，`AM#18`）。 | 不需要。 |
| InternalsVisibleTo、EmbeddedResources | 不存在：slnx 里没有（`TA:ThisAssembly.slnx:1-12`），NuGet 上 `ThisAssembly.InternalsVisibleTo`、`ThisAssembly.EmbeddedResources` 也查不到。同族只有上表这些，外加旧的 `MSBuilder.ThisAssembly.*`。 | — | 题目列举的这两个包并不存在。 |

## 5. 架构与工程对比

### 5.1 生成器与增量缓存

| | ThisAssembly | AssemblyMeta |
|---|---|---|
| 接口 | 3 个 `IIncrementalGenerator`，从 v1.1.0 起 | 7 个 `IIncrementalGenerator` |
| 增量输入 | AdditionalTexts 合并 AnalyzerConfigOptions，投影成值元组，再合并 ParseOptions（`TA:…ConstantsGenerator.cs:20-71`）。Strings 另有 `CompilationProvider.Select(language)` 和 `SourceText`（`TA:…StringsGenerator.cs:19-49`）。Resources 有 `Collect` 和一个 `WithComparer`（`TA:src/ThisAssembly.Resources/ResourcesGenerator.cs:19-43`）。 | 只有一条载荷字符串和一个 `int` 语言版本（`AM:…GeneratorHost.cs:21-29`） |
| 输出粒度 | Constants 每个常量一个文件，Resources 每个目录一个文件，Strings 每个 resx 一个文件 | 每个包一个文件 |
| 每次执行的成本 | 每次都 `Template.Parse`，再 `ParseCompilationUnit` 加 `NormalizeWhitespace`（`TA:…ConstantsGenerator.cs:100-116`） | Base64 解码，然后 `StringBuilder` 发射 |
| 缓存测试 | 没有 | 七个生成器都有 |

判断：两边的管道都不会因为普通的代码编辑失去缓存。差别在于 AssemblyMeta 的输入更窄、有测试锁住，而且每次执行更省。这是工程质量上的差别，不是用户能感知的卖点。两边都没有测量数据。

### 5.2 数据通路

- ThisAssembly 把 MSBuild 项交给 AdditionalFiles，元数据走 `build_metadata`。分号转成 `|`，再由生成器还原（roslyn#51692）；换行在 targets 里转成 `` `n ``（`TA:…Constants.targets:51-68`、`TA:…ConstantsGenerator.cs:37-39`）。
- AssemblyMeta 在 MSBuild 任务里把数据编码成带长度前缀的 Base64，放进一个 `CompilerVisibleProperty`。理由是 Roslyn 不能枚举 `build_metadata`（[dotnet/roslyn#60595](https://github.com/dotnet/roslyn/issues/60595)），而且要避开 editorconfig 截断（`AM:docs/架构.md:23-33`）。代价是要多带一个编译任务 DLL（§3.2 第 4 条），resx 内容在 MSBuild 阶段就被固定下来（§3.2 第 7 条）。

### 5.3 MSBuild 集成与打包

| | ThisAssembly 2.1.6 | AssemblyMeta 1.0.0-preview |
|---|---|---|
| props 与 targets | `build/netstandard2.0/`（`TA:src/Directory.targets:14-17`） | `buildTransitive/` |
| 防止分析器继续外流 | targets 里对自身的 `PackageReference` 设 `PrivateAssets=all`（例如 `TA:…Constants.targets:17`） | props 里同样处理（`AM:…Constants.props:13-14`） |
| analyzer 路径 | `analyzers/dotnet/roslyn4.0/cs` | `analyzers/dotnet/cs` |
| 编译任务 | 没有，全用属性函数 | 每个子包带 `tasks/netstandard2.0/AssemblyMeta.Tasks.dll` 和 Core |
| 依赖 | 子包依赖 Constants；Constants 依赖 Microsoft.CSharp 和 System.Threading.Tasks.Extensions，后者用 ILRepack 合并 | 子包零依赖，元包依赖七个子包 |
| 多目标 framework | 包只有 netstandard2.0 资产，消费方 TFM 不受限 | 同左。Resources、Strings 在外层跨目标构建里跳过（`'$(IsCrossTargetingBuild)' != 'true'`） |
| SourceLink | 用 SDK 内置的；自身包有嵌入式 PDB 和 SourceLink | 用 SDK 内置的；自身包没有符号 |
| DLL 版本 | 2.1.6.0 | 全部 1.0.0.0 |

### 5.4 发布与包拓扑

- ThisAssembly：发布 GitHub Release 时触发，用 `NUGET_API_KEY` 推送。workflow 会拒绝 `42.42*` 这类开发版本号（`TA:.github/workflows/publish.yml:47-62`）。开发包推到 Sleet CI feed。元包不含 Vsix。
- AssemblyMeta：推 `v*` 标签触发，校验推送者白名单和版本一致性，通过 OIDC 换取短期 key，先推子包再推元包（`AM:.github/workflows/release.yml`）。
- SponsorLink 对 CI 的影响：现在没有，v2 时期也没有（§5.6）。OSMF 对 CI 也没有技术影响。它的影响在法务和采购环节：许可证扫描器看到的是自定义许可文件，不是 SPDX 表达式；VS 安装时会弹出许可接受（nuspec 的 `requireLicenseAcceptance`，见 [nuspec 参考](https://learn.microsoft.com/nuget/reference/nuspec)）。

### 5.5 测试、诊断、文档站

- 测试矩阵见 §3.1 第 8 条与 §3.2 第 6 条。上游胜在平台广度（三平台加格式检查），AssemblyMeta 胜在深度（缓存断言、真实打包、用打好的包跑样例）。
- 诊断：AssemblyMeta 有 11 个码，加一条没有编号的 MSBuild 警告（`AssemblyMetaVisibility` 无法识别）。上游 3 个码，命名冲突时生成器崩溃。
- 文档：上游是英文 README（根目录和各包）加 Jekyll 页面，项目页在 clarius.org。AssemblyMeta 有中文使用说明、架构、契约、ADR 和 docfx 站点，`verify.ps1` 会做文档站冒烟。

### 5.6 SponsorLink 与 OSMF 的源码级证据

| 期间 | 版本 | 机制 | CI 是否受影响 | 证据 |
|---|---|---|---|---|
| 2023-01-24 到 2023-08-11 | v1.2.0 到 v1.3.1 | 引用 `Devlooped.SponsorLink` 包，每个子包一个 `SponsorLinker`（analyzer 兼 generator）。changelog 记载过把网络检查超时缩到 250ms。用户报告过构建出 `error SL01`。 | 未核实 | `TA@b57ebee`、`TA@be7f615`；`TA:changelog.md:480,594`；`TA#210` |
| 2023-08 | v1.4.0 | Moq 4.20 引发隐私争议（`moq#1372`，414 条评论、1122 个反应；`moq#1374`、`moq#1396`；`SponsorLink#33`「Build pauses should be gone」）。ThisAssembly 随后移除 SponsorLink（`TA#256`）。 | — | `TA:changelog.md:373`；`TA@b2ef1ef` |
| 2024-06-18 到 2025-10-16 | v2.0.0 到 v2.0.14 | SponsorLink v2 以源码形式放在 `src/SponsorLink`。离线校验 `~/.sponsorlink/github/*.jwt`。只有检测到 VS 或 Rider 的环境变量时，analyzer 才报 TA 前缀诊断，生成器才给生成成员加 `[Obsolete]`。宽限期 30 天。自动同步只在 VS 内、非设计时构建、并且用户自己开启 autosync 时运行。 | **不受影响** | `TA@v2.0.14:src/SponsorLink/SponsorLink/SponsorLink.cs:37-51`；`…/SponsorLinkAnalyzer.cs:38-39`；`…/buildTransitive/Devlooped.Sponsors.targets:21,36,106-124`；`TA@v2.0.14:src/ThisAssembly.Constants/ConstantsGenerator.cs:111`；`TA@v2.0.14:src/SponsorLink/SponsorLink.Analyzer.targets:17-22`；`TA@v2.0.14:src/Directory.props:25`；`TA#352` 正文第 2 条「CLI or CI builds will never check for sponsorship」 |
| 2025-10-16 至今 | v2.1.0 到 v2.1.6 | 删掉全部 SponsorLink 代码；包许可改成 `OSMFEULA.txt`，并要求接受许可。营收用户须付费：在 GitHub Sponsors 选 OSMF 档，起价 $10。自行从源码编译的二进制不受约束。没有任何技术检查。 | 不受影响 | `TA@7ab64af`（`TA#470`）；`TA:src/Directory.props:17-18,38`；`TA:src/Directory.Build.targets:215-222`；`TA:osmfeula.txt:12-22,42-49`；`TA#480`；[作者公告](https://www.cazzulino.com/osmf.html)（原话：SponsorLink v2 「virtually no traction」） |
| HEAD 残留 | — | 四处 `Import …Devlooped.Sponsors.targets` 带 `Exists` 条件，但这个文件在仓库和 nupkg 里都不存在，是死引用。另有 `FundingPackageId` 五处、`FundingGrace`，测试里还有 `SponsorLinkNoInstallGrace`、`SL04` 抑制和 `NoWarn TA100`。 | — | `TA:src/ThisAssembly.Constants/ThisAssembly.Constants.targets:2,20`；`TA:src/Directory.props:29`；`TA:src/ThisAssembly.Tests/ThisAssembly.Tests.csproj:23,110,113`；`TA:src/ThisAssembly.Tests/Tests.cs:8` |

社区争议对 ThisAssembly 本身的影响：`TA#352` 有 22 条评论，14 个反应全是 👎。人类第二大贡献者 viceroypenguin（12 次提交，做过增量生成器改造和 Resources 包）在 2024-10-07 表示已把项目锁在 1.4.3，「Will likely develop a personal alternative」。此后上游再没有合并过外部 PR。

### 5.7 贡献门槛与治理

- 项目实际由一个人维护：kzu 占人类提交的 95%。NuGet owner 是 clarius、Devlooped、kzu 三个账号。
- 需要签 CLA。外部 PR 上会出现 CLAassistant 机器人（见 `TA#526`）。组织级贡献指南写明「必须签 CLA 才合并」，并要求 PR 先对应一个打了「help wanted」标签的 issue（devlooped/.github 的 `.github/contributing.md`）。CLA 文本页（cla-assistant.io/devlooped）本次请求返回 503，没有读到条款。贡献前务必先读。
- 经济属性：贡献按 CLA 授权，进入一个对营收用户收维护费的二进制发行物。贡献者自己是否要付费取决于是否属于营收用途，本次没有逐条核对。
- 接受率与响应见 §1.2：历史接受率高，但节奏是批量的，单个 issue 的首次回复可能拖几个月，还有 stale 机器人自动关闭。

## 6. 决策

### Q1 AssemblyMeta 的价值与差异化

站得住、而且难以被上游抹平的只有一条：**许可**。没有 EULA，不用接受许可，营收用户不付费。只有在意这一点的组织会因此改选 AssemblyMeta。这个细分场景真实存在，但范围窄。原因有两个：上游 2.0.14 永久是 MIT；OSMF 本身也允许自行编译，不用付费（`TA:osmfeula.txt:42-45`）。

**边界正确性和工程纪律**（§3.1）是真优势，但都是 bug 级别。上游修掉只要几周到几个月，不足以支撑长期差异。

README 表里的 Strings、public 形态、命名空间三行是取舍，应如实写成取舍。另外三行要删除或改正（§2）。

### Q2 继续投入，还是转向给上游贡献

建议**停止功能扩张，不归档，增量精力投给上游**。理由：

1. 不值得追平功能。AssemblyMeta 没有采用；上游 6 年、532 star、约 84 个依赖仓库，并且被 GitInfo 依赖。追平本地化、Vsix、Kind 这些功能，也换不来用户。
2. 不建议整体转向上游。AssemblyMeta 的设计分叉（编译期 Strings、字段、RootNamespace、AM 诊断）与上游写明的选择相反（`TA:src/visibility.md`），改了会破坏现有用户，几乎不可能被接受。能贡献的只有 bug 修复。上游还有门槛：单人维护、CLA、成批处理。近 24 个月只出现过一个外部 PR，至今未合；最近一次合并外部 PR 是 27 个月前。
3. 低成本保留的价值：在本次调研范围内，它是覆盖 ThisAssembly 七个子包能力面、纯 MIT 且仍在维护的实现，可以自用或给有许可约束的雇主用，也能作为正确性参照，用来给上游提 bug。部分重叠的工具（例如 Nerdbank.GitVersioning 也会生成 `ThisAssembly` 类，见 `TA#124`）本次没有系统调研。P0 收尾（§8）工作量小，主要是配置和文档。

决策门：设一个复盘日（建议 2027-01-24，最晚 2027-04-24）。届时如果各包每版本下载仍在约 150 次以内，也没有外部 issue、PR 或 star，就标为维护模式或归档。如果出现外部用户，再投入英文文档站，以及 Strings 的可选格式化方法（作为新增成员，不改 ADR 0003）。

如果你的目标偏向影响力，边际时间应主要花在上游 PR 上。如果目标是拥有一个可控的 MIT 工具，维持 AssemblyMeta 的低维护即可。两种目标下，全面扩张都不划算。

### Q3 继续开发时吸收什么、不吸收什么

吸收（成本低，有依据）：

| 项 | 依据 |
|---|---|
| analyzer 改放 `analyzers/dotnet/roslyn4.11/cs`，同步改 PackageLayoutTests | `TA:src/Directory.props:9-10`；dotnet/sdk#20793 |
| Core、Tasks、七个 Generator 的程序集版本随包；评估合并 Core 以减少同名副本 | ThisAssembly 2.1.6 的 DLL 版本是 2.1.6.0；§3.2 第 4 条 |
| CI 三平台矩阵，加 `dotnet format --verify-no-changes` | `TA:.github/workflows/build.yml:38-61,93-108` |
| 自身包嵌入 PDB，打开 SourceLink | `TA:src/Directory.Build.props:64,78-81` |
| 英文 README 和包描述，诊断文案改英文或中英双语 | 上游各包 README |
| Project 在 `SourceControlInformationFeatureSupported` 时依赖 `InitializeSourceControlInformation` | `TA:src/ThisAssembly.Project/ThisAssembly.Project.targets:18-21` |
| 可以考虑：AssemblyInfo 增加可选的额外特性（作为新增成员） | `TA:…AssemblyInfo.targets:44-52`；按契约属于次版本 |

明确不吸收：

- OSMF、SponsorLink 或任何赞助、遥测检查。这是唯一能长期站住的差异。
- 运行时 ResourceManager 版的 Strings（ADR 0003）。
- 默认放全局命名空间。
- Scriban 模板，以及每个常量一个文件。
- Vsix：需求接近基线，而且依赖 VS SDK。
- Constant 的 `Root` 改挂（`AM#14` 已决）。
- Prerequisites：上游已停。
- `42.42.*` 版本号与 Sleet feed。
- 吸收上游代码时，MIT 要求保留「Copyright (c) Daniel Cazzulino and Contributors」声明（`TA:license.txt:3`）。本次没有发现 AssemblyMeta 复制上游源码的证据。

### Q4 停止开发时贡献回上游、单独成文

PR 或 issue 候选（都与设计取向无关）：

1. 分支前缀剥离：`TA:src/ThisAssembly.Git/ThisAssembly.Git.targets:89`。`TA#526` 已经在修。在它下面附上 §3.1 第 1 条的复现，推动合并，不要另提重复 PR。
2. 短提交号截断：`…Git.targets:58`，改成按长度取最小值。
3. Azure DevOps 和 Jenkins 的 PR 号：`…Git.targets:18,42`。
4. TA002 文案与实际判断的版本不一致：`TA:src/ThisAssembly.Constants/ConstantsGenerator.cs:83-87`。
5. 关键字转义，以及把命名冲突从抛异常改成诊断：`TA:src/ThisAssembly.Constants/Model.cs:39-58,103-106`。
6. 默认文本扩展名去掉 `.resx`：`TA:src/ThisAssembly.Resources/ThisAssembly.Resources.props:4`（运行时失败的复现见 `AM#25`）。
7. 换行统一成 `\n`：`…ConstantsGenerator.cs:93-98,114,129-134`。可能有争议，先开 issue。
8. 清理 SponsorLink 死代码（§5.6 最后一行）。

按上游指南，先开 issue，等「help wanted」标签。提 PR 前读 CLA 条款。

单独成文：

1. 设计说明：用单个 `CompilerVisibleProperty` 承载 Base64 载荷，绕过 editorconfig 截断和 `build_metadata` 无法枚举的限制；增量输入只有字符串和整数；用 `trackIncrementalGeneratorSteps` 断言缓存。材料取自 `AM:docs/架构.md`、`GeneratorHost.cs`、`GeneratorTestHost.cs`。
2. 把 [thisassembly-parity.md](thisassembly-parity.md) 整理成 ThisAssembly 边界行为参考（可译成英文）。它记录了上游文档没写的大量边界行为。

## 7. 顺带发现的错误

本仓文档：

1. `AM:README.md:37`：「SponsorLink 检查 CI」不对。v2 只在 IDE 里生效，2.1.0 起已删除（§5.6）。
2. `AM:README.md:39`：把 ThisAssembly 写成「Source Generator」，有误导。它从 1.1.0 起就是增量生成器。
3. `AM:README.md:40`：ThisAssembly 同样支持 file-based apps（`TA:readme.md:274-278`）。确定性的说法需要收窄到跨 OS 换行。
4. `AM:README.md:14`：「（空闲）」意义不明，像规划期的残留。
5. `AM:CHANGELOG.md:5,36`：「NuGet 上还没有这些包」，2026-10-23 发布后已经不成立（NuGet registration 记录的发布日期是 2026-10-23）。而 `AM:tests/AssemblyMeta.Packaging.Tests/ChangelogTests.cs:21` 断言这句话必须存在，改文档时要一起改测试。
6. `AM:README.md:51-62,87-91`：「包规划」的优先级和「里程碑」还是规划期写法，建议精简。
7. 发布包内 DLL 的版本元数据：AssemblyVersion 是 1.0.0.0，InformationalVersion 是 `1.0.0+787e764`，与包版本 `1.0.0-preview` 不一致。
8. `thisassembly-parity.md:504,570,589` 写赞助诊断码未核实。本文已经核实：2.1.x 没有任何赞助诊断；2.0.x 是 TA 前缀的 IDE 诊断。按要求没改那份文件。
9. 本次调研说明里的仓库地址 `https://github.com/Skymly/Skymly/AssemblyMeta` 多了一段，远端实际是 `https://github.com/Skymly/AssemblyMeta`。

许可：

10. README 对 ThisAssembly 许可的描述应改成：源码 MIT；2.1 起 NuGet 包附 OSMF EULA，安装时须接受，营收用户付维护费，没有技术强制，自行编译不受约束。证据见 §2 第 1 行。本仓自身许可没有发现问题：LICENSE 是 MIT，nuspec 是 `license type="expression"` MIT，不要求接受许可。

上游（可顺手提交）：

11. TA002 文案写 C# 8.0，实际判断 C# 10（`TA:src/ThisAssembly.Constants/ConstantsGenerator.cs:83-87`）。
12. `TA:src/visibility.md:10` 写的是「static readonly properties」，实际产出 `=>` 表达式体属性（`TA:src/ThisAssembly.Constants/CSharp.sbntxt:29,34`）。
13. `TA:changelog.md:42` 把 `TA#467` 列为已修，但它是被 stale 机器人以 not_planned 关闭的，维护者没有回复。
14. SponsorLink 死引用（§5.6）。

## 8. 后继动作清单

P0（在发布 1.0.0 或对外推广之前）：

1. 按 §2 改写 README 差异化表：删掉或改正第 1、3、4 行，删掉第 2 行，补上 §3 的真实优势和劣势。
2. 修正 CHANGELOG.md 第 5、36 行，同步修改 `ChangelogTests.cs:21`。删掉 README.md:14 的「（空闲）」。
3. 让 Core、Tasks、Generator 的程序集版本随 `AssemblyMetaPackageVersion`。
4. analyzer 改放 `analyzers/dotnet/roslyn4.11/cs`，同步改 PackageLayoutTests 与 `docs/架构.md` 打包一节。
5. `verify.yml` 增加 ubuntu-latest 和 macos-latest。

P1：

6. 英文 README 和 NuGet 包描述；诊断文案改英文或双语。
7. 在 VS 和 Rider 里实测「只改 resx」时 Strings 是否滞后，根据结果写进使用说明，或调整通路。
8. Project 前置 `InitializeSourceControlInformation`，并补测试。
9. 自身包嵌入 PDB，打开 SourceLink。

上游（你本人操作）：

10. 在 `TA#526` 附上复现结果：ThisAssembly.Git 2.1.6 加 SDK 10.0.303，`Branch` 为 `s/heads/feature/demo`。
11. 读 CLA 条款后，按 §6 Q4 的顺序先开 issue，从第 2 到第 6 项开始。

决策门：

12. 2027-01-24 复盘 NuGet 每版本下载（扣掉约 150 次的基线）、外部 issue、PR、star，按 §6 Q2 的规则决定维护模式或归档。

## 9. 局限与未核实

- 两边都没有做性能或缓存命中的测量。§5.1 的结论来自代码结构。
- §3.2 第 4、7、8 条是推断，需要实测。第 4 条要在 VS 或 msbuild.exe 里，同一节点混用两个版本。第 7 条要在 VS 或 Rider 里只改 resx。第 8 条要只引用 Project 包并点名 `SourceRevisionId`。
- SponsorLink v1（1.2 到 1.3）是否跳过 CI，没有核实。
- CLA 条款没有读到（cla-assistant 返回 503）。
- 没有系统调研 ThisAssembly 以外的替代品，比如 Nerdbank.GitVersioning、GitInfo。
- GitHub dependents 是 GitHub 给的近似值，只覆盖公开仓库。NuGet 下载包含 CI 还原和爬虫流量，§1.2 的基线是经验区间，不是精确扣除。
- AssemblyMeta 的下载量要等发布满一周以上再看。
