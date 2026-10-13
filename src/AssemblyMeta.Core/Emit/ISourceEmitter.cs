using AssemblyMeta.Core;
using System.Collections.Generic;

namespace AssemblyMeta.Core.Emit;

/// <summary>按语言把模型写成源码。VB/F# 实现此接口后注册到 <see cref="EmitterRegistry"/>。</summary>
public interface ISourceEmitter
{
    SourceLanguage Language { get; }

    EmitResult EmitAssemblyInfo(AssemblyInfoModel model, int csharpLanguageVersion);

    EmitResult EmitConstants(ConstantsModel model, int csharpLanguageVersion);

    EmitResult EmitProject(ConstantsModel model, int csharpLanguageVersion);

    EmitResult EmitMetadata(ConstantsModel model, int csharpLanguageVersion);

    EmitResult EmitGit(GitModel model, int csharpLanguageVersion);

    EmitResult EmitResources(ResourcesModel model, int csharpLanguageVersion);

    EmitResult EmitStrings(ConstantsModel model, int csharpLanguageVersion);
}

/// <summary>语言到 emitter 的分派表。未注册的语言返回 null，生成器应跳过而不是输出错误语言。</summary>
public sealed class EmitterRegistry
{
    public static EmitterRegistry Shared { get; } = new(new ISourceEmitter[] { new CSharpSourceEmitter() });

    private readonly Dictionary<SourceLanguage, ISourceEmitter> _emitters;

    public EmitterRegistry(IEnumerable<ISourceEmitter> emitters)
    {
        _emitters = new Dictionary<SourceLanguage, ISourceEmitter>();
        if (emitters is null)
        {
            return;
        }

        foreach (var emitter in emitters)
        {
            _emitters[emitter.Language] = emitter;
        }
    }

    public ISourceEmitter? TryResolve(SourceLanguage language)
        => _emitters.TryGetValue(language, out var emitter) ? emitter : null;
}
