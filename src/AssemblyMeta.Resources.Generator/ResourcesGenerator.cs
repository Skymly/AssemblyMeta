using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.Resources;

[Generator(LanguageNames.CSharp)]
public sealed class ResourcesGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_ResourcesPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => GeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Resources.g.cs", SourceKind.Resources);
}
