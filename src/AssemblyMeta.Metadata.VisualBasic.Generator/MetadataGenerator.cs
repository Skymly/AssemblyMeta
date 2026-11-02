using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.Metadata.VisualBasic;

[Generator(LanguageNames.VisualBasic)]
public sealed class MetadataGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_MetadataPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => VisualBasicGeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Metadata.g.vb", SourceKind.Metadata);
}