using System;
using System.Collections.Generic;
using System.IO;
using AssemblyMeta.Core;
using Microsoft.Build.Framework;

namespace AssemblyMeta.Tasks;

/// <summary>
/// 填充 RepositoryCommit / RepositorySha / RepositoryRoot / RepositoryUrl / RepositoryBranch。
/// 供 InitializeGitInformation 调用，不负责生成器载荷。
/// </summary>
public sealed class ResolveGitInformation : Microsoft.Build.Utilities.Task
{
    public string? QueriesEnabled { get; set; }

    public string? RepositoryCommit { get; set; }

    public string? RepositorySha { get; set; }

    public string? RepositoryRoot { get; set; }

    public string? RepositoryUrl { get; set; }

    public string? RepositoryBranch { get; set; }

    public string? SourceRevisionId { get; set; }

    public string? PrivateRepositoryUrl { get; set; }

    public string? PublishRepositoryUrl { get; set; }

    public string? SourceBranchName { get; set; }

    public ITaskItem[] SourceRoots { get; set; } = Array.Empty<ITaskItem>();

    public ITaskItem[] Ci { get; set; } = Array.Empty<ITaskItem>();

    [Output]
    public string Commit { get; set; } = "";

    [Output]
    public string Sha { get; set; } = "";

    [Output]
    public string Root { get; set; } = "";

    [Output]
    public string Url { get; set; } = "";

    [Output]
    public string Branch { get; set; } = "";

    public override bool Execute()
    {
        try
        {
            var roots = new List<GitSourceRoot>();
            foreach (var item in SourceRoots ?? Array.Empty<ITaskItem>())
            {
                roots.Add(new GitSourceRoot(
                    item.ItemSpec ?? "",
                    item.GetMetadata("SourceControl"),
                    item.GetMetadata("NestedRoot"),
                    item.GetMetadata("BranchName")));
            }

            var ci = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var item in Ci ?? Array.Empty<ITaskItem>())
            {
                var key = item.ItemSpec ?? "";
                if (key.Length == 0)
                {
                    continue;
                }

                ci[key] = item.GetMetadata("Value");
            }

            var result = GitInformation.Resolve(new GitResolveRequest
            {
                QueriesEnabled = TaskSupport.IsTrue(QueriesEnabled),
                RepositoryCommit = RepositoryCommit,
                RepositorySha = RepositorySha,
                RepositoryRoot = RepositoryRoot,
                RepositoryUrl = RepositoryUrl,
                RepositoryBranch = RepositoryBranch,
                SourceRevisionId = SourceRevisionId,
                PrivateRepositoryUrl = PrivateRepositoryUrl,
                PublishRepositoryUrl = TaskSupport.IsTrue(PublishRepositoryUrl),
                SourceBranchName = SourceBranchName,
                SourceRoots = roots,
                Ci = ci,
                FileExists = File.Exists,
                ReadFile = path => File.ReadAllText(path),
            });
            Commit = result.Commit;
            Sha = result.Sha;
            Root = result.Root;
            Url = result.Url;
            Branch = result.Branch;
            return !Log.HasLoggedErrors;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }
}
