using AssemblyMeta.Core;
using AssemblyMeta.Generators;
using Microsoft.CodeAnalysis;

namespace AssemblyMeta.Project.VisualBasic;

[Generator(LanguageNames.VisualBasic)]
public sealed class ProjectGenerator : IIncrementalGenerator
{
    public const string PayloadKey = "build_property.AssemblyMeta_ProjectPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
        => VisualBasicGeneratorHost.Register(context, PayloadKey, "AssemblyMeta.Project.g.vb", SourceKind.Project);
}