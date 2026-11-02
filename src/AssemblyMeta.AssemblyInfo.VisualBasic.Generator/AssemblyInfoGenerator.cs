using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.AssemblyInfo.VisualBasic;

[Generator(LanguageNames.VisualBasic)]
public sealed class AssemblyInfoGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_AssemblyInfoPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => VisualBasicGeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Info.g.vb", SourceKind.AssemblyInfo);
}