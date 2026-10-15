using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.Git;

[Generator(LanguageNames.CSharp)]
public sealed class GitGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_GitPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => GeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Git.g.cs", SourceKind.Git);
}
