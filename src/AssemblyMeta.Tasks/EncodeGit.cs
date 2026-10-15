using System;
using AssemblyMeta.Core;
using Microsoft.Build.Framework;

namespace AssemblyMeta.Tasks;

/// <summary>把已经分好的生成值编码为生成器可读的 Base64 属性。不制造占位符，也不读 Repository*。</summary>
public sealed class EncodeGit : Microsoft.Build.Utilities.Task
{
    public string? Namespace { get; set; }

    public string? Visibility { get; set; }

    public string? Commit { get; set; }

    public string? Sha { get; set; }

    public string? Root { get; set; }

    public string? Url { get; set; }

    public string? Branch { get; set; }

    public string? Unavailable { get; set; }

    [Output]
    public string Payload { get; set; } = "";

    public override bool Execute()
    {
        try
        {
            var visibility = TaskSupport.ParseVisibility(this, Visibility);
            var model = new GitModel
            {
                Namespace = Namespace,
                Visibility = visibility,
                Commit = Commit ?? "",
                Sha = Sha ?? "",
                Root = Root ?? "",
                Url = Url ?? "",
                Branch = Branch ?? "",
                Unavailable = TaskSupport.IsTrue(Unavailable),
            };
            Payload = PayloadCodec.EncodeGit(model);
            return !Log.HasLoggedErrors;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }
}
