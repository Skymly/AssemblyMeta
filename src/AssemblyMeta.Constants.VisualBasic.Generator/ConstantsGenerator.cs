using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.Constants.VisualBasic;

[Generator(LanguageNames.VisualBasic)]
public sealed class ConstantsGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_ConstantsPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => VisualBasicGeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Constants.g.vb", SourceKind.Constants);
}