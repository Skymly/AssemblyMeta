# ThisAssembly 行为对照

调查日期：2026-10-15。上游对照面是 Git 标签 `v2.1.6`（发布于 2026-09-28，NuGet 上同名包版本 2.1.6，nuspec 记录的提交 `7e7298355e1de89fb73d89514f4b982670325c18`）。`ThisAssembly.Prerequisites` 在 NuGet 上的最新版本是 1.2.11，与 2.1.6 不是同一代。

资料范围：上游 README、各包 README、changelog、GitHub Release、NuGet nuspec，以及为核对可观察行为而读过的 v2.1.6 目标文件与生成结果模板。本仓只读 README、`docs/`、`CONTEXT.md`、`docs/adr/`、测试、包的 props/targets 和公开生成成员形状。

本文件只写调用方看得到的输入和生成物，用自己的例子。不含上游代码，不摘录上游源文件。

现状取已对齐、超越、非目标、部分、缺失、刻意不同。Gate 1 只接受前三档。刻意不同只用于尚未归类、且 README「差异化」或 ADR 已经写明的选择。

## 总表

| 能力 | 现状 | 一句话 |
|---|---|---|
| 元包聚合 | 已对齐 | 两边都把七个子包收进元包，Vsix 不在其中。 |
| 生成器只给直接引用的项目 | 已对齐 | 生成只发生在写下 PackageReference 的项目。决议见 #23。 |
| 根类型可扩展 | 已对齐 | 根类是 partial，调用方可以再写成员。 |
| 命名空间 | 已对齐 | 属性名是 AssemblyMetaNamespace。非法命名空间报 AM0005 并回退。决议见 #23。 |
| 可见性 | 超越 | public 时是 static readonly 字段，不是 get-only 属性。决议见 ADR 0003 与 #23。 |
| 只生成 C# | 已对齐 | 公开说明都只生成 C#。 |
| AssemblyInfo 成员 | 非目标 | 1.0 固定九个成员，没有值时是空字符串。不收进这九个以外的程序集特性。决议见 #13。 |
| 关闭 SDK 特性生成后仍有 Info | 已对齐 | `GenerateAssemblyInfo=false` 时常量仍在。 |
| Constant 项 | 超越 | 名字、值、类型、注释和点号嵌套对齐。关键字加 @，重名报 AM0001。决议见 #23。 |
| 多行字符串 | 已对齐 | C# 11 及以上用 raw string，否则用转义字符串。 |
| FileConstant | 超越 | 项目内文件收成嵌套常量，常量值用正斜杠。决议见 #23。 |
| 把常量挂到别的嵌套类型 | 非目标 | 1.0 不提供 Constant 的 Root。Info、Project、Git 仍由各自的生成器提供。 |
| Project 选择加入 | 已对齐 | 项、分号属性、file-based app 的 `#:property` 都能收成常量。 |
| Project 默认属性 | 已对齐 | 默认是同一组七个属性，不含目标框架短名。 |
| AssemblyMetadata | 已对齐 | 只从 MSBuild 项生成常量。源码里手写的特性不是输入。决议见 #23。 |
| Git 五个成员 | 已对齐 | 只有 Commit、Sha、Root、Url、Branch。 |
| Git 设计时占位 | 超越 | 未打开查询时五个常量是 `[pending build]`。设计时把空开关写成 false。决议见 #23。占位符不写回 Repository*，见 ADR 0001。 |
| Git 分支与短哈希 | 超越 | 拉取请求记为 pr 加编号，并多认 Azure DevOps 的 PR 号和 Jenkins 的 CHANGE_ID。短于 9 位的提交号原样当作 Sha。决议见 #23。 |
| Git 的 MSBuild 属性 | 已对齐 | 下游可以依赖 `InitializeGitInformation`，读取五个 `Repository*` 属性。 |
| SourceLink | 已对齐 | 复用 SourceLink。.NET 8 起不必再手引。查询已打开且没有提交号时警告 AM0006。决议见 #23。 |
| 嵌入资源的三个访问成员 | 超越 | 每个纳入的资源都有 Text、GetBytes、GetStream。Text 按 UTF-8 读，清单名精确打开。决议见 #23。 |
| 文本扩展名、Kind、Comment | 非目标 | 1.0 不增加扩展名列表和 Kind。纳入 Resources 的项都有 Text，开关仍是 AssemblyMetaResource。 |
| 构建中追加嵌入资源 | 已对齐 | 在 PrepareResourceNames 之前把文件加进 EmbeddedResource。决议见 #23。 |
| resx 字符串与分组 | 超越 | 编译期常量沿用 ADR 0003。键里的下划线和点号分层，路径会变成前缀。决议见 #23。 |
| `{0}` 与具名占位符 | 非目标 | 1.0 不生成带参方法。占位符留在编译期常量里，调用方自己格式化。决议见 ADR 0003。 |
| resx 名字当另一名字的前缀 | 非目标 | 1.0 不另做「跳过较短键」的诊断。冲突继续走 AM0001。 |
| Vsix | 非目标 | 1.0 不做 VSIX 清单包，也不改写 Pack。 |
| Prerequisites | 非目标 | 1.0 不做编译器版本检查包，也不在编译期发版本诊断。决议见 #23。 |
| 诊断 | 超越 | 调用方看到的是 AM0001 至 AM0011。不追上游的码。决议见 #23。 |
| 增量生成 | 已对齐 | 缓存输入是单条载荷加语言版本。七个生成器都有缓存测试。决议见 #23。少数意见：守门人投超越。 |
| 最低 MSBuild / Roslyn | 已对齐 | 编译器 API 4.11.0。SDK 10.0.303 是验证环境。编译期不检查。决议见 #18 与 #23。 |
| CI 包版本 | 非目标 | 1.0 不产生可安装的预发布包。版本只有一个来源，值为 1.0.0。 |
| 包元数据 | 超越 | 作者、readme、仓库、开发依赖和本仓标签保持现状。图标和符号包沿用 #19，都不要。决议见 #23。少数意见：质疑官把图标和符号包投成非目标。 |
| 许可 | 超越 | 纯 MIT，无维护费，安装时不要求接受另一份许可。决议见 README 差异化与 #23。 |
| 文档 | 超越 | 使用说明、架构和 docfx，只维护简体中文。决议见 #20 与 #23。少数意见：质疑官把 Jekyll 和英文文档投成非目标。 |

共 35 行。已对齐 16，部分 0，缺失 0，刻意不同 0，非目标 8，超越 11。

## 逐项详情

### 元包聚合

- 能力：元包一次引用七块生成能力。
- 可观察行为：引用元包后，直接项目同时得到 Info、Constants、Project、Metadata、Git、Resources、Strings。NuGet 依赖是这七个 2.1.6 包，没有 Vsix，也没有 Prerequisites。包描述写的是包含全部 `ThisAssembly.*`，依赖列表比这句话窄。
- 边界情形：只想要一块时，可以改引用单个子包。Vsix 必须单独引用。
- AssemblyMeta 现状：已对齐
- 证据：`src/AssemblyMeta/README.md`、`src/AssemblyMeta/AssemblyMeta.csproj`
- 来源：<https://www.nuget.org/packages/ThisAssembly/2.1.6>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly/ThisAssembly.csproj>

### 生成器只给直接引用的项目

- 能力：分析器不要流到依赖这个项目的下一个项目。
- 可观察行为：2.1.6 各子包 nuspec 都标了开发依赖。子包目标把对本包的 `PackageReference` 收成只在当前项目私有，并且不随 pack 传递。v2.1.6 发布说明写的是让元包对引用它的项目保持私有。直接引用的项目仍能看到生成成员。
- 边界情形：库项目再被别人引用时，生成器不应继续出现在那个消费者里。发布说明没有给出调用方要写的属性。
- AssemblyMeta 现状：已对齐
- 证据：`docs/架构.md` 打包一节写明元包把子包放给直接消费者，子包 props 再把直接引用收成私有；`src/AssemblyMeta.Constants/buildTransitive/AssemblyMeta.Constants.props` 对 `AssemblyMeta.Constants` 和 `AssemblyMeta` 都做了这件事。
- 来源：<https://github.com/devlooped/ThisAssembly/releases/tag/v2.1.6>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Constants/ThisAssembly.Constants.targets>

### 根类型可扩展

- 能力：生成一个可再写的根类。
- 可观察行为：默认在全局命名空间生成 partial 根类 `ThisAssembly`。各包往里面加嵌套类型。调用方可以再声明同一个 partial，补自己的成员。

输入：

```xml
<ItemGroup>
  <Constant Include="Retry.Limit" Value="3" Type="int" />
</ItemGroup>
```

生成物形状：根类是没有可见性修饰符的 `partial class ThisAssembly`，常量挂在其中的嵌套类型上。调用方可以另写一个 `partial class ThisAssembly`，补上自己的成员。点号如何变成嵌套类型，见 Constant 一节。

- 边界情形：根类默认没有可见性修饰符，在 C# 里就是 internal。
- AssemblyMeta 现状：已对齐
- 证据：`README.md` 生成入口，`docs/使用说明.md` 生成入口。根类名是 `AssemblyMeta`。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/visibility.md>

### 命名空间

- 能力：把根类放进指定命名空间。
- 可观察行为：MSBuild 属性 `ThisAssemblyNamespace` 有值时，根类放进该命名空间。空着就在全局命名空间。

输入：

```xml
<PropertyGroup>
  <ThisAssemblyNamespace>Acme.Build</ThisAssemblyNamespace>
</PropertyGroup>
```

生成物：`namespace Acme.Build;` 下面的 `partial class ThisAssembly`。

- 边界情形：设置了非空命名空间、且语言版本低于 C# 10 时，报错误 TA002，文案写成「需要 C# 8.0 或更高」，这条常量不生成。判断用的是 C# 10，文案写的是 8.0。命名空间字符串本身不合法时，公开文档没有写。
- AssemblyMeta 现状：已对齐
- 证据：`docs/使用说明.md` 的 `AssemblyMetaNamespace`；`tests/AssemblyMeta.Generator.Tests/GeneratorTests.cs` 的 `Invalid_namespace_falls_back_and_warns` 对非法命名空间报告 `AM0005`。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/visibility.md>

### 可见性

- 能力：选择 internal 常量或 public 只读属性。
- 可观察行为：`ThisAssemblyVisibility` 空着时，根类没有修饰符，常量是 `const`。设成 `public` 后，根类变成 public，原来的常量改成 `public static` 只读属性，这样引用方可以不因常量值变化而重编译。嵌套类型保持 public。想让根类 public 又保留 `const` 时，调用方自己再写一个 public partial，生成成员仍是常量。

输入：

```xml
<PropertyGroup>
  <ThisAssemblyVisibility>public</ThisAssemblyVisibility>
</PropertyGroup>
<ItemGroup>
  <Constant Include="Retry.Limit" Value="3" Type="int" />
</ItemGroup>
```

生成物形状：`public partial class ThisAssembly` 里，`Retry` 下的 `Limit` 是 `public static int Limit => 3;`。

- 边界情形：Strings 的成员本来就是属性和方法，这条开关不把它们变成 `const`。只对走常量那条路的成员生效。
- AssemblyMeta 现状：超越
- 证据：`docs/使用说明.md`；`tests/AssemblyMeta.Generator.Tests/PayloadAndEmitterTests.cs` 的 `Public_visibility_uses_static_readonly_and_namespace`；`tests/AssemblyMeta.Generator.Tests/ResourcesStringsTests.cs` 的 `Public_strings_are_static_readonly`。属性名是 `AssemblyMetaVisibility`。public 时保持 `static readonly` 字段，见 ADR 0003。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/visibility.md>

### 只生成 C#

- 能力：生成语言只有 C#。
- 可观察行为：README 写明目前只生成 C#。Strings 按编译语言挑选模板，仓库里随包的模板是 C# 那一份。
- 边界情形：Prerequisites 目标在讨论版本时提到 VB，那是版本警告的语言条件，不是会生成 VB 成员。公开文档没有 VB 或 F# 的生成示例。
- AssemblyMeta 现状：已对齐
- 证据：`README.md` 差异化与技术架构；`docs/架构.md` 多语言；`docs/adr/0002-no-language-seam-for-source-emission.md`。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/readme.md>

### AssemblyInfo 成员

- 能力：把程序集特性收成 `ThisAssembly.Info`。
- 可观察行为：README 列出九个特性：Configuration、Company、Title、Description、Product、Copyright、Version、InformationalVersion、FileVersion。对应成员挂在 `ThisAssembly.Info` 上，值来自项目里这些特性的参数。

输入：

```xml
<PropertyGroup>
  <Company>Northwind</Company>
  <AssemblyVersion>2.4.0.0</AssemblyVersion>
  <InformationalVersion>2.4.0-preview.1</InformationalVersion>
</PropertyGroup>
```

生成物：`ThisAssembly.Info.Company` 为 `Northwind`，`ThisAssembly.Info.Version` 为 `2.4.0.0`，`ThisAssembly.Info.InformationalVersion` 为 `2.4.0-preview.1`。`Version` 是程序集版本，不是带预发布标签的信息版本。

源码核实：准备目标会把 `GetAssemblyAttributes` 里、名字以 `System.Reflection.Assembly` 开头、又不是 `AssemblyMetadata` 的特性都收进来。成员名是去掉 `Assembly` 前缀和 `Attribute` 后缀之后的部分。因此文档没写的特性，只要 SDK 把它放进特性项，也会出现，例如项目写了商标时会出现 `ThisAssembly.Info.Trademark`。

- 边界情形：默认 SDK 类库不会多出 `Info.Trademark`。公司、产品和标题为空时，SDK 会填上程序集名，所以这三项仍是成员；说明和版权为空时不是成员。`GenerateAssemblyInfo=false` 且商标属性为空字符串时，商标开关是打开的，但 SDK 不把空字符串放进特性清单，也不生成 `Info.Trademark`。项目文件里写成一个空格时，MSBuild 读入后长度仍是 0，结果相同。只有求值之后还剩下内容，才会生成该成员；商标为 `Northwind Mark` 时，`Info.Trademark` 的值就是这串字。若求值结果被固定成一个 U+0020 空格，成员会出现，但生成常量是空字符串。这次用 ThisAssembly.AssemblyInfo 2.1.6，在 SDK 10.0.303 的 net10.0 上构建过，0 警告 0 错误。手写在 C# 源码里的特性不是这项的输入；只有 MSBuild 特性项会被收进来。没有用一份只在源码里写特性的项目跑过。
- AssemblyMeta 现状：非目标
- 证据：`src/AssemblyMeta.AssemblyInfo/README.md`；`docs/使用说明.md`；`tests/AssemblyMeta.Generator.Tests/GeneratorTests.cs` 的 `AssemblyInfo_generator_emits_nine_constants`。九个成员仍会生成，没有值时是空字符串，不回填程序集名。非目标只指不收进这九个以外的程序集特性，例如商标。决议见 https://github.com/Skymly/AssemblyMeta/issues/13。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.AssemblyInfo/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.AssemblyInfo/ThisAssembly.AssemblyInfo.targets>

### 关闭 SDK 特性生成后仍有 Info

- 能力：`GenerateAssemblyInfo=false` 时 Info 仍生成。
- 可观察行为：关掉 SDK 的 AssemblyInfo 生成后，包仍通过收集特性项得到上述常量。changelog 把「设成 false 之后生成器不再跑」记为已修复。README 还说，用别的方式提供这些特性时，Info 上仍会有对应常量。
- 边界情形：修复说明针对的是 MSBuild 特性项。手写 C# 特性是否算「别的方式」，公开示例没有单独给出。
- AssemblyMeta 现状：已对齐
- 证据：`src/AssemblyMeta.AssemblyInfo/README.md`；`docs/架构.md`；`src/AssemblyMeta.AssemblyInfo/buildTransitive/AssemblyMeta.AssemblyInfo.targets`。
- 来源：<https://github.com/devlooped/ThisAssembly/issues/407>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/changelog.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.AssemblyInfo/readme.md>

### Constant 项

- 能力：把 `Constant` 项收成强类型常量。
- 可观察行为：`Include` 里的点号变成嵌套类型。`Value` 可以用 MSBuild 属性，因此能吃环境变量和命令行。`Type` 按原样写进生成代码，不写则是 `string`。类型和值是否构成合法 C#，由调用方保证。`Comment` 成为该成员的文档摘要。

输入：

```xml
<PropertyGroup>
  <RetryLimit>3</RetryLimit>
</PropertyGroup>
<ItemGroup>
  <Constant Include="Retry.Limit" Value="$(RetryLimit)" Type="int" Comment="重试次数" />
</ItemGroup>
```

生成物：`ThisAssembly.Constants.Retry.Limit` 的类型是 `int`，值是 `3`。

- 边界情形：可以写 `bool`、`long`、`double` 等。注释放进 XML 文档时按 XML 文本转义。非法标识符字符收成下划线，连续下划线折叠成一个，开头不能做标识符时前面再加下划线；C# 关键字不加 `@`，`class` 这类名字会原样留下。同一个名字既要当常量、又要当下一层类型时，生成直接失败，没有诊断码。`ItemType` 是 MSBuild 项类型名，用来只接收 `Constant` 项；调用方指定生成类型时写的是 `Type`。
- AssemblyMeta 现状：超越
- 证据：`docs/使用说明.md`；`tests/AssemblyMeta.Generator.Tests/GeneratorTests.cs` 的 `Constants_generator_emits_nested_typed_members_and_duplicate_diagnostic`。同名冲突本仓报 `AM0001` 并跳过。关键字加 `@` 见 `tests/AssemblyMeta.Generator.Tests/PayloadAndEmitterTests.cs` 的 `Sanitizer_collapses_invalid_characters_and_escapes_keywords`。差异化表和 ADR 没有把关键字或重名诊断写成有意分叉。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Constants/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/changelog.md>

### 多行字符串

- 能力：多行常量值保持完整。
- 可观察行为：多行值在目标语言版本支持 raw string（C# 11，语言版本 11 及以上）时用 raw string，否则用转义字符串。changelog v2.0.14 记过多行 raw string 的格式修复。目标在交给生成器前把换行规范成 LF。
- 边界情形：值里含分号时，目标会做一层替换再交给生成器，避免编辑器配置把分号截断。调用方写在 `Value` 里的分号应仍出现在常量值中。
- AssemblyMeta 现状：已对齐
- 证据：`docs/使用说明.md`；`tests/AssemblyMeta.Generator.Tests/PayloadAndEmitterTests.cs` 的 `Multiline_string_uses_raw_literal_and_roundtrips` 与 `Older_language_version_does_not_emit_raw_strings`。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Constants/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/changelog.md>

### FileConstant

- 能力：给项目里的文件生成路径常量。
- 可观察行为：`FileConstant` 收一组文件。项目内文件，或带 `Link` 的文件，会变成 `ThisAssembly.Constants` 下的嵌套常量。成员名来自相对目录加文件名、去掉扩展名，分隔符换成点。常量值是这条相对路径再加上扩展名。项目外且没有 `Link` 的文件跳过。

输入：

```xml
<ItemGroup>
  <FileConstant Include="notes/about.txt" />
</ItemGroup>
```

生成物：`ThisAssembly.Constants.notes.about`，值是带扩展名的相对路径 `notes/about.txt`（分隔符见边界）。

- 边界情形：同一路径去掉扩展名后若撞车，公开文档没有说明，两个文件会争同一个成员名。相对路径按 `RelativeDir` 原样拼接，目标不把反斜杠收成正斜杠。没有另在 Windows 上用发布包跑一次构建；分隔符结论来自 v2.1.6 目标本身。
- AssemblyMeta 现状：超越
- 证据：`docs/使用说明.md`；`src/AssemblyMeta.Constants/README.md`；`tests/AssemblyMeta.Generator.Tests/PayloadAndEmitterTests.cs` 的 `FileConstant_uses_forward_slashes_and_link`。本仓把常量值收成正斜杠。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Constants/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Constants/ThisAssembly.Constants.targets>

### 把常量挂到别的嵌套类型

- 能力：Constant 可以不挂在 `Constants` 下。
- 可观察行为：Constant 项有 `Root` 和 `RootComment`。Git、Project、AssemblyInfo 都用 `Root="."`，于是成员落在 `ThisAssembly.Git`、`ThisAssembly.Project`、`ThisAssembly.Info`，而不是 `ThisAssembly.Constants.*`。`RootComment` 替换该嵌套类型的摘要。不写 `Root` 时，README 示例落在 `ThisAssembly.Constants`。
- 边界情形：包 README 没有把这两个元数据写成调用方文档。changelog 2.0.3 写过自定义根摘要。
- AssemblyMeta 现状：非目标
- 证据：`src/AssemblyMeta.Constants/buildTransitive/AssemblyMeta.Constants.props` 只把 Value、Type、Comment、Link 交给生成器。1.0 不提供 Root。Info、Project、Git 仍由各自的生成器提供。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Constants/ThisAssembly.Constants.targets>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/changelog.md>

### Project 选择加入

- 能力：点名之后才把项目属性收成常量。
- 可观察行为：`ProjectProperty` 项的 `Include` 是属性名，可选 `Comment`。属性 `$(ProjectProperty)` 按原样并进同一组项，分号分隔多个名字。file-based app 写 `#:property ProjectProperty=某个属性名`。生成成员是 `ThisAssembly.Project.属性名`，值是该属性求值之后的结果。

输入：

```xml
<PropertyGroup>
  <Warehouse>east</Warehouse>
  <ProjectProperty>Warehouse</ProjectProperty>
</PropertyGroup>
```

也可以写成项：`<ProjectProperty Include="Warehouse" Comment="仓库代码" />`。

生成物：`ThisAssembly.Project.Warehouse` 为 `east`。

- 边界情形：v2.1.3 才把 `$(ProjectProperty)` 这条路写进 changelog。准备目标会等 SourceLink 初始化之后再求值，这样属性里若引用了仓库信息，能拿到已经填好的值。
- AssemblyMeta 现状：已对齐
- 证据：`docs/使用说明.md`；`src/AssemblyMeta.Project/README.md`；`tests/AssemblyMeta.Generator.Tests/ProjectMetadataGitTests.cs` 的 `Project_generator_emits_nested_constants_and_cache_hits`。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Project/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/changelog.md>

### Project 默认属性

- 能力：不点名也会带上一组属性。
- 可观察行为：默认项是 `AssemblyName`、`RootNamespace`、`TargetFrameworkIdentifier`、`TargetFrameworkMoniker`、`TargetFrameworkVersion`、`RuntimeIdentifier`、`RuntimeIdentifiers`。changelog v2.0.13 把后两个加了进来。目标框架短名 `TargetFramework` 不在默认列表里。

生成物示例：`ThisAssembly.Project.TargetFrameworkIdentifier` 可能是 `.NETCoreApp`，`ThisAssembly.Project.RuntimeIdentifiers` 是该属性的字符串值，空属性就是空字符串。

- 边界情形：调用方可以用 MSBuild 的 Remove 去掉默认项。公开 README 的 Project 一节没有逐个列出这七个名字，名单来自 v2.1.6 的 props。
- AssemblyMeta 现状：已对齐
- 证据：`src/AssemblyMeta.Project/buildTransitive/AssemblyMeta.Project.props`；`src/AssemblyMeta.Project/README.md`；`docs/使用说明.md`。本仓把这组称为目标框架标识，短名要另外点 `TargetFramework`。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Project/ThisAssembly.Project.props>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/changelog.md>

### AssemblyMetadata

- 能力：把 `AssemblyMetadata` 项收成常量。
- 可观察行为：.NET 5 起 SDK 认识的项会生成 `ThisAssembly.Metadata.名字`。SDK 仍会写 `AssemblyMetadataAttribute`。本包读的是项目文件里的项。

输入：

```xml
<ItemGroup>
  <AssemblyMetadata Include="Channel" Value="stable" />
</ItemGroup>
```

生成物：`ThisAssembly.Metadata.Channel` 为 `stable`。点号同样嵌套，因为这条路复用常量。

- 边界情形：README 没有写 Comment。常量那条路能带 Comment，Metadata 是否把项上的 Comment 收进文档，包 README 没示例。源码手写的特性是否出现在 `ThisAssembly.Metadata`，README 没分开说。
- AssemblyMeta 现状：已对齐
- 证据：`docs/使用说明.md` 写明只读该项，源码手写特性不会出现；`tests/AssemblyMeta.Generator.Tests/ProjectMetadataGitTests.cs` 的 `Metadata_generator_emits_constants_and_cache_hits`。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Metadata/readme.md>

### Git 五个成员

- 能力：生成 `ThisAssembly.Git`。
- 可观察行为：v2.1.6 的 Git 目标只加入这五个常量：`Commit`、`Sha`、`Root`、`Url`、`Branch`。没有其它 Git 成员。`Sha` 是提交号的前 9 个字符。`Root` 把反斜杠换成正斜杠。`Url` 仅在 `PublishRepositoryUrl` 为 true，或 `RepositoryUrl` 已经有值时填充。

输入：提交号 `0123456789abcdef`，且打开了查询。

生成物：`ThisAssembly.Git.Commit` 为完整提交号，`ThisAssembly.Git.Sha` 为 `012345678`。

- 边界情形：提交号短于 9 个字符时，目标仍按固定 9 位去截。MSBuild 在长度不够时会让这个目标失败。没有另造短哈希仓库去跑，所以失败文案未记录。`Root` 只保证把反斜杠换成正斜杠，不另行补结尾斜杠；结尾斜杠取决于 SourceLink 给出的源根。
- AssemblyMeta 现状：已对齐
- 证据：`docs/使用说明.md`；`tests/AssemblyMeta.Generator.Tests/ProjectMetadataGitTests.cs` 的 `Git_generator_emits_members_pending_placeholder_and_cache_hits`。短哈希和结尾斜杠的差异记在下一行。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Git/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Git/ThisAssembly.Git.targets>

### Git 设计时占位

- 能力：设计时构建不查源码管理。
- 可观察行为：`EnableSourceControlManagerQueries` 不是 `true` 时，上述五个常量的值都是字面量 `[pending build]`。设为 `true` 后，常量改用解析到的 `Repository*` 值。这个属性来自 SourceLink。README 仍写「在 IDE 里看起来总是空的」；v2.1.6 目标写进去的是占位符，不是空字符串。
- 边界情形：占位只写在五个常量上，不写回 `Repository*`。调查当天的 SourceLink 公共 props 在该属性为空时把它设为 true；设计时关掉的是是否生成 SourceLink，不是这个查询开关。上游 Git 目标自己也不在设计时把查询开关写成 false。因此 README 的「设计时默认不查」并不由这套目标保证。属性若留空，SourceLink 会把它设成 true，常量会填真实值。Visual Studio 会不会在导入 SourceLink 之前先写成 false，本次没有在 IDE 里跑，仍未核实。
- AssemblyMeta 现状：超越
- 证据：`docs/使用说明.md`；`docs/adr/0001-git-placeholder-not-written-to-repository-properties.md`；`src/AssemblyMeta.Git/buildTransitive/AssemblyMeta.Git.props` 在设计时或 Live Unit Testing 且属性为空时写成 false；`tests/AssemblyMeta.Generator.Tests/GitInformationTests.cs` 的 `User_branch_wins_over_ci_and_head_is_not_read_when_queries_disabled`。差异化表没有单独写这一行，ADR 0001 只约束占位符不写回 `Repository*`。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Git/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Git/ThisAssembly.Git.targets>，<https://github.com/dotnet/sourcelink/blob/main/src/SourceLink.Common/build/Microsoft.SourceLink.Common.props>（2026-10-15 的 main，未与 2.1.6 锁定同一提交）

### Git 分支与短哈希

- 能力：分支来自 CI，否则回落本地仓库。
- 可观察行为：README 列出的 CI 是 GitHub Actions、Azure DevOps、AppVeyor、TeamCity、Travis CI、Circle CI、GitLab CI、Buddy、Jenkins。有拉取请求编号时，分支写成 `pr` 加编号，例如 `pr123`。没有 CI 分支时，用 SourceLink 源根上的分支名；.NET SDK 9 之前这条可能为空，于是再读 `.git/HEAD`。`.git` 若是指向 gitdir 的文件（worktree），v2.1.4 / v2.1.5 会沿着该指针找 HEAD。子模块源根被排除。没有可用的 SourceLink 地址时，构建警告 TA001。

GitHub 的 `refs/pull/15/merge` 得到 `pr15`。标签引用会去掉 `refs/tags/` 前缀，得到标签名。

- 边界情形：v2.1.6 目标里，Azure DevOps 只改写 `BUILD_SOURCEBRANCH`，没有读取 `SYSTEM_PULLREQUEST_PULLREQUESTID`。Jenkins 只读 `GIT_LOCAL_BRANCH`，没有 `CHANGE_ID`。因此「只要 CI 给了 PR 号就写成 pr」这句话，对这两个系统没有落在已读的目标里。
- AssemblyMeta 现状：超越
- 证据：`docs/使用说明.md`；`tests/AssemblyMeta.Generator.Tests/GitInformationTests.cs` 的 `Ci_pull_requests_become_pr_numbers`、`Sha_is_first_nine_or_full_when_shorter`、`Head_and_worktree_gitdir_supply_branch_when_ci_is_empty`、`Nested_source_root_is_ignored`。本仓还会把 Azure DevOps 的 PR 号和 Jenkins 的 `CHANGE_ID` 收成 `pr` 加编号；提交号短于 9 位时原样当作 Sha。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Git/ThisAssembly.Git.targets>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/changelog.md>

### Git 的 MSBuild 属性

- 能力：给下游目标同一组仓库属性。
- 可观察行为：目标名 `InitializeGitInformation`。它依赖 SourceLink 的 `InitializeSourceControlInformation`。之后可以读 `RepositoryCommit`、`RepositorySha`、`RepositoryRoot`、`RepositoryUrl`、`RepositoryBranch`。名字与 nuget pack 使用的属性对齐。
- 边界情形：没有 SourceLink 提供者时仍给出 TA001，对应属性可能为空。
- AssemblyMeta 现状：已对齐
- 证据：`docs/使用说明.md`；`src/AssemblyMeta.Git/README.md`；`src/AssemblyMeta.Git/buildTransitive/AssemblyMeta.Git.targets`。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Git/readme.md>

### SourceLink

- 能力：Git 信息来自 SourceLink，而不是包自己再实现一套源码管理。
- 可观察行为：包说明要求按托管站点引用 `Microsoft.SourceLink.*`。README 写明从 .NET 8 SDK 起 SourceLink 已内置，不必再加包引用。
- 边界情形：内置之后若仓库种类没有对应提供者，TA001 仍会出现，常量在打开查询后可能是空字符串。
- AssemblyMeta 现状：已对齐
- 证据：`docs/使用说明.md`；`docs/架构.md`。缺提交号且查询已打开时，本仓警告 `AM0006`，见 `tests/AssemblyMeta.Generator.Tests/ProjectMetadataGitTests.cs`。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Git/readme.md>

### 嵌入资源的三个访问成员

- 能力：用类型访问嵌入资源的文本、字节和流。
- 可观察行为：`EmbeddedResource` 按相对路径变成 `ThisAssembly.Resources` 下的嵌套类型。文本资源有 `Text`，第一次读取后缓存在静态字段里。`GetBytes` 与 `GetStream` 每次重新打开。找不到以该相对路径结尾的清单名时，运行时抛出 `InvalidOperationException`。

输入：

```xml
<ItemGroup>
  <EmbeddedResource Include="notes/about.txt" />
</ItemGroup>
```

生成物：`ThisAssembly.Resources.notes.about.Text`、`GetBytes()`、`GetStream()`。

路径里的 `\` 和 `/` 在查找清单名时换成点，再在程序集清单名里做结尾匹配。`GetBytes` 按流的长度一次性读入。

同一去掉扩展名的路径若有多个文件，叶子名改用扩展名去掉点之后的部分，避免两个成员同名。

- 边界情形：README 没有写编码。源码核实文本读取没有单独指定编码。流若没有长度，`GetBytes` 的读法会失败；公开文档没写。
- AssemblyMeta 现状：超越
- 证据：`docs/使用说明.md`；`src/AssemblyMeta.Resources/README.md`；`tests/AssemblyMeta.Generator.Tests/ResourcesStringsTests.cs` 的 `Resources_generator_reads_manifest_and_renames_conflicts`。本仓对每个纳入的资源都生成 `Text`，用清单资源名精确打开，`Text` 按 UTF-8 读取并缓存，`GetBytes` 复制整段流。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Resources/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Resources/ResourcesGenerator.cs>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Resources/CSharp.sbntxt>

### 文本扩展名、Kind、Comment

- 能力：决定哪些嵌入资源带 `Text`，以及成员文档写什么。
- 可观察行为：属性 `EmbeddedResourceStringExtensions` 是用 `|` 分隔的扩展名列表，比较时忽略大小写。可以追加，也可以整段覆盖。某一项的 `Kind` 为 `Text`（忽略大小写）时，即使扩展名不在列表里也生成 `Text`。`Comment` 有内容时，用作该成员的文档摘要；没有时，摘要写成指向相对路径的一行。

输入：

```xml
<PropertyGroup>
  <EmbeddedResourceStringExtensions>.txt|.md|.csv</EmbeddedResourceStringExtensions>
</PropertyGroup>
<ItemGroup>
  <EmbeddedResource Include="query.kql" Kind="Text" Comment="值班查询" />
</ItemGroup>
```

生成物：`query` 这个嵌套类型有 `Text`，文档摘要是「值班查询」。`notes/about.csv` 也会有 `Text`。`notes/logo.png` 只有 `GetBytes` 和 `GetStream`。

v2.1.6 标签和已发布 `ThisAssembly.Resources` 2.1.6 包内 props 的默认值都是 `.txt|.cs|.sql|.json|.md|.resx`。同标签 README 写的默认值没有 `.resx`。包内 props 与标签一致，与 README 不一致。

- 边界情形：准备资源的目标把每个 `EmbeddedResource` 的纳入标记写成 true，会盖掉项目里事先写的 false。README 没有排除开关。`.resx` 因此默认带 `Text`，会和 Strings 同时看见同一个文件。运行时按路径改成点号之后做清单名后缀匹配，查找串仍保留 `.resx`。SDK 编译进程序集后清单名以 `.resources` 结尾，对不上。ThisAssembly.Resources 2.1.6、SDK 10.0.303、net10.0：根目录 `Messages.resx` 的清单名是 `程序集名.Messages.resources`，`Text` 抛出 `InvalidOperationException`，消息是找不到以 `Messages.resx` 结尾的资源。`notes/Greeting.resx` 同样找不到以 `notes.Greeting.resx` 结尾的资源。同一次运行里 `notes/about.txt` 能读到，清单名以 `.txt` 结尾。没有测自定义清单名、非 SDK 项目和带文化的卫星 resx。见 https://github.com/Skymly/AssemblyMeta/issues/25 。
- AssemblyMeta 现状：非目标
- 证据：`docs/使用说明.md` 与 `src/AssemblyMeta.Resources/README.md` 写了 Comment 的冲突改名，没有扩展名列表，也没有 Kind。排除用 `AssemblyMetaResource=false`，强制纳入 resx 用 `AssemblyMetaResource=true`。`.resx` 默认不进入 Resources。1.0 不增加扩展名列表和 Kind；纳入的项都有 Text。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Resources/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Resources/ThisAssembly.Resources.props>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Resources/ThisAssembly.Resources.targets>

### 构建中追加嵌入资源

- 能力：目标可以在生成前动态加入嵌入资源。
- 可观察行为：在 `PrepareEmbeddedResources` 之前把文件加入 `EmbeddedResource`，这些文件会参与生成。该目标依赖 `PrepareResourceNames`。changelog v2.0.10 把这个扩展点写成公开契约。

输入：

```xml
<Target Name="AddShiftNotes" BeforeTargets="PrepareEmbeddedResources">
  <ItemGroup>
    <EmbeddedResource Include="notes/$(Configuration).txt" />
  </ItemGroup>
</Target>
```

生成物：Debug 构建出现 `ThisAssembly.Resources.notes.Debug`。

- 边界情形：加得太晚，清单名或纳入标记已经错过，该项不会出现。
- AssemblyMeta 现状：已对齐
- 证据：`docs/使用说明.md` 要求在 `PrepareResourceNames` 之前加入 `EmbeddedResource`。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Resources/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/changelog.md>

### resx 字符串与分组

- 能力：把中性 resx 收成 `ThisAssembly.Strings`。
- 可观察行为：作为 `EmbeddedResource`、类型为 Resx、没有文化、仍要生成资源、并且已有清单名的文件会参与。带文化的卫星 resx 不参与。键名里的下划线变成嵌套类型。`| : ; > <` 会先换成下划线。没有格式占位符的项是 `public static string` 属性，运行时用清单资源名创建资源管理器再取字符串，不是编译期 `const`。有评论元素时，文档摘要只写评论，换行折成空格，尖括号转义。没有评论元素时，摘要改用资源值本身，换行同样折成空格。评论元素存在但为空时，摘要才写成 `= "值"`，值里的换行被去掉。公开 README 示例在评论下面还多了一行 `=> "原文"`，与 2.1.6 的生成结果不一致。带 `type` 属性的 data 项整项跳过，包括把 type 写成字符串的情况。文件名不变成额外的一层类型：不同 resx 的键都挂在 `Strings` 下，用各自的清单资源名取字符串。

输入：`Messages.resx` 里键 `Shop_Closed`，值 `Closed today`，评论 `打烊`。

生成物：`ThisAssembly.Strings.Shop.Closed`，文档摘要是「打烊」。

- 边界情形：两个文件若有相同键，会在同一个 `Strings` 下各写一份，调用方会碰到重复成员。只有 mimetype、没有 type 的项仍会进入生成。空名字或没有 value 的项跳过。
- AssemblyMeta 现状：超越
- 证据：`docs/使用说明.md`；`src/AssemblyMeta.Strings/README.md`；`tests/AssemblyMeta.Generator.Tests/ResourcesStringsTests.cs` 的 `Strings_generator_emits_constants_and_cache_hits`、`String_names_drop_strings_file_prefix_and_split_underscores`、`Resx_reader_keeps_string_values_and_skips_binary`。本仓默认是编译期常量；键里的点号也分层；文件路径会变成前缀，只有末段名为 `Strings` 时去掉这一段。带 type 或 mimetype 的非字符串项跳过并警告 `AM0007`。`type` 为 `string` 或 `System.String` 的项保留。编译期常量、占位符留在字面量、卫星 resx 不参与，已由 ADR 0003 定为有意差异。路径前缀和点号分层仍未单独立成决议，所以这一行保持部分。
- 来源：<https://www.nuget.org/packages/ThisAssembly.Strings/2.1.6>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Strings/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Strings/ThisAssembly.Strings.targets>

### `{0}` 与具名占位符

- 能力：格式串生成带参数的方法。
- 可观察行为：NuGet 描述和 README 都写明，带格式参数的字符串生成参数个数相符的方法，具名占位符会变成更可读的参数名。源码核实的规则如下。

占位符形如 `{名字}` 或 `{名字:格式}`。花括号里不能再含花括号。全部是数字时，生成位置参数方法，参数类型是 `object`，参数名是 `arg` 加该数字。全部不是数字时，生成具名参数方法，参数名就是占位符里的名字。调用时用当前区域性做 `string.Format`。带格式说明的参数会先按该格式转成字符串，再填进格式串。

输入一，键 `Pack_Ready`，值 `{0} ready for {1}.`：

生成物：`ThisAssembly.Strings.Pack.Ready(object arg0, object arg1)`。

输入二，键 `Hello`，值 `Hello {who}`：

生成物：`ThisAssembly.Strings.Hello(object who)`。

输入三，键 `Ships`，值 `Ships on {day:yyyy-MM-dd}`：

生成物：`ThisAssembly.Strings.Ships(object day)`。调用时 `day` 先按 `yyyy-MM-dd` 格式化，再填进句子。

位置参数和具名参数混在同一条值里时，这条既不算位置格式，也不算具名格式，生成结果里没有这个成员。已读的生成结果只跳过该项，没有另给诊断码。

- 边界情形：`{{0}}` 里的 `{0}` 会按占位符处理，转义花括号挡不住。没有用生成器跑过这条输入，结论来自 2.1.6 的匹配规则。两个以上具名占位符的形参顺序与书写中的首次出现顺序一致，不是字母序；同一个名字只留一个形参，格式说明不单独占参数，也不改变该名字的位置。`{1}` 写在 `{0}` 前面时，位置参数是 arg1、arg0。这次用 ThisAssembly.Strings 2.1.6，在 SDK 10.0.303 上生成过。README 示例把评论和格式原文同时写进摘要；2.1.6 在有评论时只写评论。位置参数和具名参数混用时不生成成员，也没有诊断；混用没有放进这次构建，仍是源码结论。
- AssemblyMeta 现状：非目标
- 证据：`docs/adr/0003-compile-time-strings-and-static-readonly-fields.md`；README「差异化」。`docs/使用说明.md` 与 `src/AssemblyMeta.Strings/README.md` 写明 `{0}` 留在字符串里。`tests/AssemblyMeta.Generator.Tests/ResourcesStringsTests.cs` 把 `你好，{0}` 当成常量值执行。形参顺序的生成结果在 [#9](https://github.com/Skymly/AssemblyMeta/issues/9)。
- 来源：<https://www.nuget.org/packages/ThisAssembly.Strings/2.1.6>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Strings/Model.cs>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Strings/CSharp.sbntxt>

### resx 名字当另一名字的前缀

- 能力：避免一个键既是成员又是嵌套类型。
- 可观察行为：同一 resx 里，若某个键加上下划线之后是另一个键的前缀，例如同时有 `Shop` 和 `Shop_Ready`，则报错误 TA003，提示把较短的名字改成带 `_Title` 的形式，并且不为较短的那个键生成成员。changelog v2.1.5 对应重复成员问题。
- 边界情形：跨文件的同名键不走这条诊断。
- AssemblyMeta 现状：非目标
- 证据：本仓不另做「跳过较短键」的诊断。重名走的是 `AM0001`，见 `docs/架构.md`。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/changelog.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Strings/Model.cs>

### Vsix

- 能力：在代码和 VSIX 清单里共用一组清单属性。
- 可观察行为：独立包 `ThisAssembly.Vsix` 2.1.6，不在元包里。清单 `source.extension.vsixmanifest` 用 `|%CurrentProject%;属性名|` 引用下面这些 MSBuild 属性。装有 VS SDK 且能展开清单时，代码侧只生成五个常量：`ThisAssembly.Vsix.Id`、`Name`、`Description`、`Publisher`、`Version`。`Name` 取清单里的 DisplayName。`Product` 和 `Language` 只有 MSBuild 目标，没有对应的代码常量。没有 VS SDK 时，这五个常量也不生成。默认值：

| 属性 | 默认 |
|---|---|
| VsixID | `$(PackageId)`，否则 `$(AssemblyName)` |
| VsixVersion | `$(Version)` |
| VsixDisplayName | `$(Title)` |
| VsixDescription | `$(Description)` |
| VsixProduct | `$(Product)` |
| VsixPublisher | `$(Company)` |
| VsixLanguage | `$(NeutralLanguage)`，否则 `en-US` |

`Pack` 在装有 VS SDK 且 `PackageId` 非空时被改成创建 VSIX。包依赖 `ThisAssembly.Constants` 2.1.6，以及 `Microsoft.VSSDK.BuildTools`（依赖版本 18.5.40034，include 为 none）。标签比其它包多一个 `vsix`。已发布包里是 `build/netstandard2.0` 下的 props/targets，没有分析器 DLL。

- 边界情形：它服务的是 Visual Studio 扩展清单，不生成普通项目用得到的成员。本仓包规划没有这块，也没有 ADR 说不做。是否对齐，取决于要不要覆盖 VSIX 项目；对「把项目和程序集事实变成常量」这条主线，它是旁路。
- AssemblyMeta 现状：非目标
- 证据：`README.md` 包规划没有 Vsix。1.0 不做 VSIX 清单包，不引用 VS SDK，不改写 Pack。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Vsix/readme.md>，<https://www.nuget.org/packages/ThisAssembly.Vsix/2.1.6>

### Prerequisites

- 能力：在编译前检查 MSBuild 和 Roslyn 是否够新。
- 可观察行为：NuGet 包 `ThisAssembly.Prerequisites` 1.2.11 的描述是确保引用项目使用兼容的编译器 API。它是开发依赖，许可表达式为 MIT，不要求接受许可，作者 Daniel Cazzulino，标签 `dotnet roslyn`，项目页与仓库指向 ThisAssembly。nuspec 没有依赖。2.1.6 元包和已读的子包 nuspec 都不依赖它。

已打开的 1.2.11 nupkg 含 `build/netstandard2.0` 下的 props 与 targets、图标和占位程序集，没有 readme，也没有 OSMF 许可文件。检查规则与 v2.1.6 标签上的同名目标相同：MSBuild 低于 16.5 则构建错误；C# 或 VB 的编译器 API 低于 4.0 则警告 THIS001。4.0 这个数来自增量生成器接口在 Roslyn 4.0.1 出现。F# 不参与这道警告。2026-10-15 拆开的 2.1.6 各 nupkg 都不含这段目标，nuspec 也不依赖该包。changelog 在 1.2.12 记过移除这层依赖。它不生成 `ThisAssembly` 成员。

- 边界情形：当前 SDK 已高于 MSBuild 16.5 和 Roslyn 4.0。对齐这个包不会补上任何生成成员。要不要另做一条最低版本警告，矩阵不代决定。
- AssemblyMeta 现状：非目标
- 证据：本仓无证据。`Directory.Build.props` 的 `LangVersion` 是本仓自己的编译设置，不是给调用方的门槛。
- 来源：<https://www.nuget.org/packages/ThisAssembly.Prerequisites/1.2.11>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Prerequisites.targets>

### 诊断

- 能力：输入不合法时给出调用方能看到的诊断。
- 可观察行为：TA001，警告，Git 没有可用的 SourceLink 地址，随 `ThisAssembly.Git` 2.1.6 的目标发出。TA002，错误，设置了命名空间且语言版本低于 C# 10，文案却写成需要 C# 8.0。TA003，错误，resx 键名被用作另一个键的下划线前缀。THIS001 只在 `ThisAssembly.Prerequisites` 1.2.11 里，2.1.6 已发布包的文件列表里没有它。混合的 `{0}` 与 `{name}` 不生成成员，也没有诊断码。仓库属性 `FundingGrace` 为 30，changelog 写过宽限期之后生成代码时发警告；这次没有跑一次编译，赞助诊断码未核实。
- 边界情形：常量名字冲突和短提交号不会变成诊断，前者让生成失败，后者让 Git 目标失败。
- AssemblyMeta 现状：超越
- 证据：`docs/架构.md` 列出 `AM0001` 至 `AM0011`。测试覆盖见 `tests/AssemblyMeta.Generator.Tests/GeneratorTests.cs`、`ProjectMetadataGitTests.cs`、`ResourcesStringsTests.cs`。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Git/ThisAssembly.Git.targets>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Prerequisites.targets>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/changelog.md>

### 增量生成

- 能力：生成器按增量接口运行，输入不变时不重复出结果。
- 可观察行为：公开 README 只说使用 Roslyn source generator，没有写缓存契约。changelog 1.1.0 起改为增量生成器。2.1.6 里 Constants、Resources、Strings 都是增量生成器，输入是附加文件、分析器能看见的属性和语言版本。Info、Project、Metadata、Git、Vsix 不另带分析器 DLL，它们把常量项交给 Constants。调用方没有开关。本仓 README 差异化把上游写成 Source Generator、把自己写成严格缓存；两边都是增量生成器，剩下的差别是缓存输入。本仓用单条载荷加语言版本，见 `docs/架构.md`。
- 边界情形：没有用两次构建去测量上游的缓存命中。Prerequisites 把最低编译器写成 4.0，理由是增量接口，但 2.1.6 的包不再带这道检查。
- AssemblyMeta 现状：已对齐
- 证据：`docs/架构.md`；`tests/AssemblyMeta.Generator.Tests/GeneratorTests.cs` 的 `Unchanged_analyzer_options_reuse_incremental_output` 与 `Constants_generator_reuses_incremental_output`；Git、Project、Metadata、Resources、Strings 的测试里都有缓存断言。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/changelog.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Constants/ConstantsGenerator.cs>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Resources/ResourcesGenerator.cs>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Strings/StringsGenerator.cs>

### 最低 MSBuild / Roslyn

- 能力：公开的最低工具版本。
- 可观察行为：README 没有写最低 SDK。与版本有关的公开句子只有：.NET 8 SDK 起不必再手引 SourceLink；Project 的 file-based app 写法指向现行 SDK 文档。包的依赖组目标框架是 .NET Standard 2.0，这是包本身的 TFM，不是调用方项目的最低 TFM。仓库目标文件里的门槛见 Prerequisites 一节。
- 边界情形：2.1.6 已发布包的文件列表里没有 THIS001。最低版本只存在于已停更的 Prerequisites 1.2.11。
- AssemblyMeta 现状：已对齐
- 证据：本仓无证据。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/src/ThisAssembly.Prerequisites.targets>

### CI 包版本

- 能力：分支和 PR 产生可试用的包。
- 可观察行为：README 的 Dogfooding 一节写 CI feed `https://pkg.kzu.app/index.json`，PR 包为 `42.42.42-pr` 加编号，分支包为 `42.42.42-` 加分支名、点、提交数。v2.1.6 的 `build.yml` 实际传入的前缀是 `42.42.` 加 GitHub run number，版本标签是当前 ref。非 main 的分支和 PR 会被改成前缀 `42.42.0`，后缀是整理后的 ref 再加 run number。标签发布时版本就是去掉 `v` 的标签，例如 2.1.6。`publish.yml` 在 GitHub Release 发布后推到 nuget.org，并拒绝文件名里带 `42.42` 的包。构建矩阵是 ubuntu-latest、macOS-latest、windows-latest。非 CI 的本地包版本是 `42.` 加自 2024-03-15 起的天数、再加当天分钟数，与 README 里的 `42.42.42` 不是同一条规则。
- 边界情形：feed 在调查当天是否仍能按 README 或按工作流还原这些包，没有去 feed 上拉取。工作流与 README 的版本公式不一致，以工作流为准描述 2.1.6 会打出的包，以 README 为准描述公开说明书。
- AssemblyMeta 现状：非目标
- 证据：`Directory.Build.props` 里的包版本是单一来源。1.0 不产生可安装的预发布包，CI 只构建、测试、打包。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/.github/workflows/build.yml>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/.github/workflows/publish.yml>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/.github/workflows/os-matrix.json>

### 包元数据

- 能力：NuGet 上调用方能看到的包装信息。
- 可观察行为：下表来自 2026-10-15 拉取的 nuspec。符号包一列是对 `https://api.nuget.org/v3-flatcontainer/{id}/{version}/{id}.{version}.snupkg` 的 HEAD 结果。

| 包 | 版本 | 作者 | 许可 | 接受许可 | 标签 | 图标 | readme | 仓库 |
|---|---|---|---|---|---|---|---|---|
| ThisAssembly | 2.1.6 | Daniel Cazzulino | 文件 OSMFEULA.txt | 是 | dotnet roslyn | 包内 icon.png | 有 | git，分支 v2.1.6，上述提交 |
| ThisAssembly.AssemblyInfo | 2.1.6 | 同上 | 同上 | 是 | dotnet roslyn | 同上 | 有 | 同上 |
| ThisAssembly.Constants | 2.1.6 | 同上 | 同上 | 是 | dotnet roslyn | 同上 | 有 | 同上 |
| ThisAssembly.Project | 2.1.6 | 同上 | 同上 | 是 | dotnet roslyn | 同上 | 有 | 同上 |
| ThisAssembly.Metadata | 2.1.6 | 同上 | 同上 | 是 | dotnet roslyn | 同上 | 有 | 同上 |
| ThisAssembly.Git | 2.1.6 | 同上 | 同上 | 是 | dotnet roslyn | 同上 | 有 | 同上 |
| ThisAssembly.Resources | 2.1.6 | 同上 | 同上 | 是 | dotnet roslyn | 同上 | 有 | 同上 |
| ThisAssembly.Strings | 2.1.6 | 同上 | 同上 | 是 | dotnet roslyn | 同上 | 有 | 同上 |
| ThisAssembly.Vsix | 2.1.6 | 同上 | 同上 | 是 | dotnet roslyn vsix | 同上 | 有 | 同上 |
| ThisAssembly.Prerequisites | 1.2.11 | 同上 | 表达式 MIT | 否 | dotnet roslyn | 包内 icon.png | nuspec 未列 readme | 分支 v1.2.11，提交 `c243b86bdea80d440bd3a16101aeab69af7646cb` |

项目页都是 `https://clarius.org/ThisAssembly`。2.1.6 各包的 releaseNotes 指向仓库 `changelog.md`。版权是 Daniel Cazzulino and Contributors。描述与上文各节的第一句一致；Strings 的描述另外写明具名参数，例如 `Hello {name}`。AssemblyInfo 的 nuspec 描述比 README 少了 Description 和 Copyright 两行，以 README 和上面的九个成员为准。

2026-10-15 对十个包的 `https://api.nuget.org/v3-flatcontainer/{id}/{version}/{id}.{version}.snupkg` 做 HEAD，全部 404。不能据此说没有符号。拆开的 nupkg 里没有单独的 pdb。Constants 的分析器 DLL 带嵌入式 PDB，信息版本是 `2.1.6+7e7298355`，并带仓库地址 `https://github.com/devlooped/ThisAssembly`。仓库构建把调试信息设为嵌入，并打开 SourceLink。PDB 内的 SourceLink 文档表是压缩的，这次没有解出来。nuspec 的 `repository` 提交与这串信息版本对得上。

包布局：生成器在 `analyzers/dotnet/roslyn4.0/cs/`，Constants、Strings 各一个 DLL，Resources 的文件名是 `ThisAssembly.Resource.dll`。Info、Project、Metadata、Git、Vsix 和元包没有自己的分析器 DLL。props/targets 在 `build/netstandard2.0/`，不是 `buildTransitive`。`lib/netstandard2.0/_._` 是占位。包上有 NuGet 仓库签名。

- 边界情形：符号服务器上是否另有一份 PDB，未再查。旧包 `MSBuilder.ThisAssembly.Vsix`、`MSBuilder.ThisAssembly.Project`、`MSBuilder.ThisAssembly.Metadata` 的作者是 Mobile Essentials，不在 v2.1.6 源目录里，与这十个包的行为关系未核实。
- AssemblyMeta 现状：超越
- 证据：`src/AssemblyMeta.Packaging.targets` 与 `src/AssemblyMeta/AssemblyMeta.csproj`：作者 Skymly，许可表达式 MIT，仓库 `https://github.com/Skymly/AssemblyMeta`，有 readme，`IncludeSymbols` 为 false，没有 PackageIcon，也没有打开 SourceLink。标签是 `roslyn;source-generator;constants;msbuild` 以及各子包 csproj 上的附加标签。开发依赖为 true。分析器放在 `analyzers/dotnet/cs`，props/targets 放在 `buildTransitive`，见 `docs/架构.md`。
- 来源：各包 nuspec，页面入口 <https://www.nuget.org/packages/ThisAssembly/2.1.6> 与同版本子包页面。

### 许可

- 能力：调用方安装包时看到的许可条件。
- 可观察行为：2.1.6 各包的许可文件是 OSMF EULA，安装时要求接受许可。README 写明产生收入的使用者要支付开源维护费，源码另有许可证。仓库徽章同时指向 EULA 和 OSS 许可证。Prerequisites 1.2.11 仍是 MIT，且不要求接受许可。
- 边界情形：赞助检查会不会在编译时再发一条诊断，见未核实清单。
- AssemblyMeta 现状：超越
- 证据：README「差异化」写明本仓纯 MIT，无维护费。`src/AssemblyMeta.Packaging.targets` 的许可表达式是 MIT。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/osmfeula.txt>，<https://www.nuget.org/packages/ThisAssembly/2.1.6>

### 文档

- 能力：调用方能读到的说明。
- 可观察行为：v2.1.6 没有 `docs/` 目录。说明在根 README（含各包摘录、可见性、Dogfooding）、各包 README、changelog 和 GitHub Release。`pages.yml` 用 Jekyll 渲染仓库，主题是 jekyll-theme-slate，项目页写的是 `https://clarius.org/ThisAssembly`。2.1.6 的 nupkg 除 Prerequisites 外都带 `readme.md`。
- 边界情形：本次没有打开 clarius.org 确认站点是否仍在更新。AssemblyInfo 的包描述短于它的 README。
- AssemblyMeta 现状：超越
- 证据：`docs/使用说明.md`、`docs/架构.md`、`docs/index.md`。根 README 写文档站用 `dotnet docfx docs/docfx.json`。
- 来源：<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/readme.md>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/_config.yml>，<https://github.com/devlooped/ThisAssembly/blob/v2.1.6/.github/workflows/pages.yml>

## 未核实清单

下面这些没有足够的结果，不能当成已核对的行为。

1. issue #467（changelog v2.1.3）所修的 AOT / 单文件问题，修复后调用方能看到的变化。只看到「已修」。调试生成里仍有读取程序集位置的一行；会不会在 AOT 下报警，没有跑发布。
2. 赞助检查的诊断码和文案。仓库属性 `FundingGrace` 为 30，changelog 写过宽限期后发警告。没有跑一次编译。
3. 嵌入式 PDB 里的 SourceLink 文档表。它是压缩的，没有解压。snupkg 地址返回 404 之后，符号服务器上是否另有 PDB，没有再查。
4. Visual Studio 设计时会不会在 SourceLink 把查询开关设为 true 之前，先把它写成 false。没有在 IDE 里跑。
5. （已核实，见「{0} 与具名占位符」）两个以上具名占位符的形参顺序与书写中的首次出现顺序一致。同一名字配两种格式、以及 `{b} {a} {b}` 这种重复之后再出现新名字，这次没有生成。
6. （已核实，见「AssemblyInfo 成员」）默认 SDK 项目不会多出 `Info.Trademark`。空商标不会变成成员。非空商标会。只写在源码里的空 `AssemblyTrademark` 没有跑过。
7. 只写在 C# 源码里的程序集特性。目标的输入是 MSBuild 特性项，不扫描源码。没有用这样的项目跑过。
8. （已核实，见「文本扩展名、Kind、Comment」）`.resx` 被当成文本之后，按 `.resx` 后缀找不到以 `.resources` 结尾的清单名。`Text` 抛出找不到资源的异常。`.txt` 在同一次运行里能读到。
9. 提交号短于 9 个字符时，Git 目标失败的具体文案。没有跑构建。
10. CI feed `https://pkg.kzu.app/index.json` 在调查当天能否还原包。
11. 旧包 `MSBuilder.ThisAssembly.*` 与 v2.1.6 的行为关系。
