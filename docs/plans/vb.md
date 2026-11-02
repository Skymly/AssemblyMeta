# VB 路线图

目的地：VB 项目引用现有七个子包后，能编译并读到与 C# 相同的七块值。C# 生成源码不变。发出去的版本是 `1.2.0`。推标签仍走现有发布流程，这条路线不改发布方式。

契约见 [ADR 0005](../adr/0005-visual-basic-is-a-separate-emitter.md)。地图是 [AssemblyMeta VB 路线图](https://github.com/Skymly/AssemblyMeta/issues/41)。

## 已定

- 新的 `VisualBasicSourceEmitter`，以及标了 `LanguageNames.VisualBasic` 的生成器。不恢复语言分派。`SourceEmission.Emit` 仍只服务 C#。
- 根类型是 `Friend Partial Module AssemblyMeta`。嵌套类型是 `Class`，成员 `Shared`。调用方扩展时写 `Partial Module`。
- `AssemblyMetaVisibility` 仍是 `internal` 或 `public`。VB 写成 `Friend` 或 `Public`。`Const` 与 `Shared ReadOnly` 的选用规则与 C# 的 `const` / `static readonly` 相同。
- `Type` 仍写 C# 别名。VB 只映射能做常量的那一组。映射表以外报诊断并跳过。
- 多行值用常量拼接，不用 raw string。关键字用方括号。VB 的重名比较忽略大小写。
- 生成文件是 `.g.vb`。程序集放在 `analyzers/dotnet/roslyn4.11/vb`。C# 目录和 C# 生成器引用都不动。
- 七个区域一起做。Resources 是唯一有方法体的部分，生成代码要在 `Option Strict On` 下编译，不写 `Imports`。

## 不做

- F#。F# 不跑这套增量生成器。
- Vsix、`Root` / `RootComment`、`Kind` 和扩展名列表。
- 卫星 resx、带参格式化方法。ADR 0003。
- 商标和其他九个字段以外的程序集特性。
- 编译期编译器版本检查。
- file-based VB。`#:package` 只有 C#。
- 一个编译同时出两种语言。

## 和既有决议

- [ADR 0002](../adr/0002-no-language-seam-for-source-emission.md)：新语言是新的生成器和新的实现。
- [AssemblyMeta 1.0 路线图](https://github.com/Skymly/AssemblyMeta/issues/6) 把「生成 VB 或 F#」标在那次验收之外。F# 仍在外。VB 从这里开始。
- [重叠备忘录](../research/thisassembly-overlap-decision.md) 建议停止功能扩张。这一项是例外。[2027-01-24 复盘](https://github.com/Skymly/AssemblyMeta/issues/38) 仍管其它扩张。
- [上游贡献候选清单](https://github.com/Skymly/AssemblyMeta/issues/39) 不并进这条路线。

## 顺序

1. 契约已经写在 ADR 0005。实现时不要另开一套可见性或 `Type` 语法。
2. 在 Core 里做 VB 发射器和标识符规则。Core 仍不引用 Roslyn。
3. 七个 VB 生成器。每个子包把对应程序集和 `AssemblyMeta.Core.dll` 放进 `analyzers/dotnet/roslyn4.11/vb`。
4. 用 `VisualBasicCompilation` 断言生成源码能编译。`samples/` 加一个 VB 项目，`./eng/verify.ps1` 跑它。
5. 使用说明、契约、中英 README。使用说明仍是简体中文。英文只保持 `README.en.md`。
6. 包版本改为 `1.2.0`，`CHANGELOG.md` 记一节。不在这条路线里推标签。

## 验收

- 同一组 Constant、中性 resx 和嵌入资源，VB 项目读到的值与 C# 相同。
- 只差大小写的名字，以及映射表以外的 `Type`，在 VB 里有诊断，不写出非法源码。
- C# 样例的生成源码与 `1.1.0` 相同。
- `Option Strict On` 下通过。
