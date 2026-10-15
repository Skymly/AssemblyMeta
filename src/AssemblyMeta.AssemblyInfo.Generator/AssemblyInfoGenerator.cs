using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.AssemblyInfo;

[Generator(LanguageNames.CSharp)]
public sealed class AssemblyInfoGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_AssemblyInfoPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => GeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Info.g.cs", SourceKind.AssemblyInfo);
}
