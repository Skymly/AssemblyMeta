using System;
using System.Collections.Generic;
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

    [Output]
    public string GeneratedCommit { get; set; } = "";

    [Output]
    public string GeneratedSha { get; set; } = "";

    [Output]
    public string GeneratedRoot { get; set; } = "";

    [Output]
    public string GeneratedUrl { get; set; } = "";

    [Output]
    public string GeneratedBranch { get; set; } = "";

    [Output]
    public string Unavailable { get; set; } = "";

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
            foreach (var key in GitInformation.CiKeys)
            {
                ci[key] = Environment.GetEnvironmentVariable(key);
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
            });
            Commit = result.Properties.Commit;
            Sha = result.Properties.Sha;
            Root = result.Properties.Root;
            Url = result.Properties.Url;
            Branch = result.Properties.Branch;
            GeneratedCommit = result.Generated.Commit;
            GeneratedSha = result.Generated.Sha;
            GeneratedRoot = result.Generated.Root;
            GeneratedUrl = result.Generated.Url;
            GeneratedBranch = result.Generated.Branch;
            Unavailable = result.Generated.Unavailable ? "true" : "false";
            return !Log.HasLoggedErrors;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }
}
