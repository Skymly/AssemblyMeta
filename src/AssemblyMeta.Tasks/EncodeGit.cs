using System;
using AssemblyMeta.Core;
using Microsoft.Build.Framework;

namespace AssemblyMeta.Tasks;

/// <summary>把已解析的 Git 属性编码为生成器可读的 Base64 属性。查询关闭时写入占位符。</summary>
public sealed class EncodeGit : Microsoft.Build.Utilities.Task
{
    public string? Namespace { get; set; }

    public string? Visibility { get; set; }

    public string? QueriesEnabled { get; set; }

    public string? Commit { get; set; }

    public string? Sha { get; set; }

    public string? Root { get; set; }

    public string? Url { get; set; }

    public string? Branch { get; set; }

    [Output]
    public string Payload { get; set; } = "";

    public override bool Execute()
    {
        try
        {
            var visibility = TaskSupport.ParseVisibility(this, Visibility);
            var queries = TaskSupport.IsTrue(QueriesEnabled);
            var model = queries
                ? new GitModel
                {
                    Namespace = Namespace,
                    Visibility = visibility,
                    Commit = Commit ?? "",
                    Sha = Sha ?? "",
                    Root = Root ?? "",
                    Url = Url ?? "",
                    Branch = Branch ?? "",
                    Unavailable = string.IsNullOrWhiteSpace(Commit),
                }
                : new GitModel
                {
                    Namespace = Namespace,
                    Visibility = visibility,
                    Commit = GitInformation.PendingValue,
                    Sha = GitInformation.PendingValue,
                    Root = GitInformation.PendingValue,
                    Url = GitInformation.PendingValue,
                    Branch = GitInformation.PendingValue,
                    Unavailable = false,
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
