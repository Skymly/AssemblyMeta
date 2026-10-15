using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.Metadata;

[Generator(LanguageNames.CSharp)]
public sealed class MetadataGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_MetadataPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => GeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Metadata.g.cs", SourceKind.Metadata);
}
