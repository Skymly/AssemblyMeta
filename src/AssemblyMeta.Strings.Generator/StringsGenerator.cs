using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.Strings;

[Generator(LanguageNames.CSharp)]
public sealed class StringsGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_StringsPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => GeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Strings.g.cs", SourceKind.Strings);
}
