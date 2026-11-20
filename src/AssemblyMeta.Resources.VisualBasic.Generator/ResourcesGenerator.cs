using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.Resources.VisualBasic;

[Generator(LanguageNames.VisualBasic)]
public sealed class ResourcesGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_ResourcesPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => VisualBasicGeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Resources.g.vb", SourceKind.Resources);
}