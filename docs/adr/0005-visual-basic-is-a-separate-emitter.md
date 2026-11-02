# VB 是另一套生成器，不是语言分派

VB 项目要看到 `AssemblyMeta`，就单独做 `VisualBasicSourceEmitter` 和标了 `LanguageNames.VisualBasic` 的生成器。不恢复按语言分派的注册表。`SourceEmission.Emit` 仍只服务 C#。这是 [ADR 0002](0002-no-language-seam-for-source-emission.md) 已经写下的路。

MSBuild 项和 `AssemblyMetaVisibility` 不改。`internal` / `public` 在 VB 里写成 `Friend` / `Public`。根类型是 `Friend Partial Module AssemblyMeta`；嵌套类型是 `Class`，成员 `Shared`。Module 不能嵌套。`const` 写成 `Const`，`static readonly` 写成 `Shared ReadOnly`。`public` 时仍不能写进特性参数。关键字用 `[Keyword]`。

`Type` 仍写 C# 别名。VB 只映射能做常量的那一组，例如 `int` 写成 `Integer`，`string` 写成 `String`，`float` 写成 `Single`。映射表以外的类型报诊断并跳过，不把 `int` 原样写进 VB。多行值用 `"line1" & vbLf & "line2"`，引号写成 `""`。标识符冲突在 VB 里忽略大小写，报 `AM0001`；C# 仍区分大小写。

生成文件是 `.g.vb`。VB 程序集放在 `analyzers/dotnet/roslyn4.11/vb`，旁边再放 `AssemblyMeta.Core.dll`。C# 生成器不引用 `Microsoft.CodeAnalysis.VisualBasic`。七个区域一起做。C# 生成源码不变。

不做 F#、Vsix、`Root` / `RootComment`、`Kind`、卫星 resx、带参格式化方法、商标和其他 `Assembly*` 特性、编译期版本检查。
