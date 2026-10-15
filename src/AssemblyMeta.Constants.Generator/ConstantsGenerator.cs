using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.Constants;

/// <summary>
/// 从 build_property.AssemblyMeta_ConstantsPayload 生成 AssemblyMeta.Constants。
/// 不读取 AdditionalFiles，也不枚举 build_metadata，保证增量步骤只比较载荷字符串。
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ConstantsGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_ConstantsPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => GeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Constants.g.cs", SourceKind.Constants);
}
