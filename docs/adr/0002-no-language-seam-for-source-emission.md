# 不为 C# 发射保留语言 seam

`ISourceEmitter` 和 `EmitterRegistry` 按语言分派，但只有 `CSharpSourceEmitter` 一个 adapter。七个 Generator 都标 `[Generator(LanguageNames.CSharp)]`，并写死 C#。注册 VB 或 F# 也接不上，生成器不会加载。载荷到源码不按语言分派：调用方交出载荷、种类和 C# 语言版本，得到源码和诊断。以后要做别的语言，是新的 Generator 和新的实现，不是往这个注册表里加一项。
