using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.Constants;

[Generator(LanguageNames.CSharp)]
public sealed class ConstantsGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_ConstantsPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => GeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Constants.g.cs", SourceKind.Constants);
}
