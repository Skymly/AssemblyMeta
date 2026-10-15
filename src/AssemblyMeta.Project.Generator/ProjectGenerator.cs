using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.Project;

[Generator(LanguageNames.CSharp)]
public sealed class ProjectGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_ProjectPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => GeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Project.g.cs", SourceKind.Project);
}
