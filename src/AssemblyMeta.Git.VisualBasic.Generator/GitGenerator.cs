using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.Git.VisualBasic;

[Generator(LanguageNames.VisualBasic)]
public sealed class GitGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_GitPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => VisualBasicGeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Git.g.vb", SourceKind.Git);
}