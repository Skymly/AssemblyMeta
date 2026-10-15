using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.AssemblyInfo;

/// <summary>
/// 从 build_property.AssemblyMeta_AssemblyInfoPayload 生成 AssemblyMeta.Info。
/// 输入只有分析器配置和语言版本，避免源码变化击穿增量缓存。
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class AssemblyInfoGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_AssemblyInfoPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => GeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Info.g.cs", SourceKind.AssemblyInfo);
}
