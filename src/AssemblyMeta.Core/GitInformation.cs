using System;
using System.Collections.Generic;
using System.IO;

namespace AssemblyMeta.Core;

/// <summary>一个 SourceRoot。NestedRoot 非空表示子模块，不作为仓库根。</summary>
public readonly record struct GitSourceRoot(string Path, string SourceControl, string NestedRoot, string BranchName);

/// <summary>解析 Git 信息的输入。文件读取留在 module 内。</summary>
public sealed class GitResolveRequest
{
    public bool QueriesEnabled { get; init; }

    public string? RepositoryCommit { get; init; }

    public string? RepositorySha { get; init; }

    public string? RepositoryRoot { get; init; }

    public string? RepositoryUrl { get; init; }

    public string? RepositoryBranch { get; init; }

    public string? SourceRevisionId { get; init; }

    public string? PrivateRepositoryUrl { get; init; }

    public bool PublishRepositoryUrl { get; init; }

    public string? SourceBranchName { get; init; }

    public IReadOnlyList<GitSourceRoot> SourceRoots { get; init; } = Array.Empty<GitSourceRoot>();

    public IReadOnlyDictionary<string, string?>? Ci { get; init; }
}

/// <summary>写回 Repository* 的五个字符串。没有不可用标志。</summary>
public sealed class GitValues
{
    public string Commit { get; init; } = "";

    public string Sha { get; init; } = "";

    public string Root { get; init; } = "";

    public string Url { get; init; } = "";

    public string Branch { get; init; } = "";
}

/// <summary>写进生成物的五个字符串，外加不可用标志。空字符串是 ""。</summary>
public sealed class GitGenerated
{
    public string Commit { get; init; } = "";

    public string Sha { get; init; } = "";

    public string Root { get; init; } = "";

    public string Url { get; init; } = "";

    public string Branch { get; init; } = "";

    public bool Unavailable { get; init; }
}

/// <summary>一次解析的两组结果。</summary>
public sealed class GitResolution
{
    public GitValues Properties { get; init; } = new();

    public GitGenerated Generated { get; init; } = new();
}

/// <summary>
/// 提交来自 SourceLink。RepositoryBranch 为空时，分支优先取 CI 环境变量，拉取请求记为 pr 加编号。
/// 读取 .git/HEAD 只在查询开启且用户字段、CI、SourceLink 都没有分支时发生。
/// 查询关闭时不读文件；生成值是占位符，属性值仍是解析结果。
/// </summary>
public static class GitInformation
{
    public const string PendingValue = "[pending build]";
    private const int ShaLength = 9;

    public static IReadOnlyList<string> CiKeys { get; } = new[]
    {
        "GITHUB_REF",
        "BUILD_SOURCEBRANCH",
        "SYSTEM_PULLREQUEST_PULLREQUESTID",
        "APPVEYOR_PULL_REQUEST_NUMBER",
        "APPVEYOR_REPO_TAG_NAME",
        "APPVEYOR_REPO_BRANCH",
        "TEAMCITY_BUILD_BRANCH",
        "TRAVIS_PULL_REQUEST",
        "TRAVIS_BRANCH",
        "CIRCLE_PR_NUMBER",
        "CIRCLE_TAG",
        "CIRCLE_BRANCH",
        "CI_COMMIT_TAG",
        "CI_MERGE_REQUEST_IID",
        "CI_EXTERNAL_PULL_REQUEST_IID",
        "CI_COMMIT_BRANCH",
        "BUDDY_EXECUTION_PULL_REQUEST_NO",
        "BUDDY_EXECUTION_TAG",
        "BUDDY_EXECUTION_BRANCH",
        "CHANGE_ID",
        "GIT_LOCAL_BRANCH",
    };

    private static readonly HashSet<string> CiKeySet = new(CiKeys, StringComparer.Ordinal);

    public static GitResolution Resolve(GitResolveRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var properties = ResolveProperties(request);
        if (!request.QueriesEnabled)
        {
            return new GitResolution
            {
                Properties = properties,
                Generated = new GitGenerated
                {
                    Commit = PendingValue,
                    Sha = PendingValue,
                    Root = PendingValue,
                    Url = PendingValue,
                    Branch = PendingValue,
                    Unavailable = false,
                },
            };
        }

        return new GitResolution
        {
            Properties = properties,
            Generated = new GitGenerated
            {
                Commit = properties.Commit,
                Sha = properties.Sha,
                Root = properties.Root,
                Url = properties.Url,
                Branch = properties.Branch,
                Unavailable = properties.Commit.Length == 0,
            },
        };
    }

    private static GitValues ResolveProperties(GitResolveRequest request)
    {
        var commit = First(request.RepositoryCommit, request.SourceRevisionId);
        var sha = First(request.RepositorySha, ToSha(commit));
        var root = First(NormalizeRoot(request.RepositoryRoot), FirstGitRoot(request.SourceRoots));
        var url = (request.RepositoryUrl ?? "").Trim();
        if (url.Length == 0 && request.PublishRepositoryUrl)
        {
            url = (request.PrivateRepositoryUrl ?? "").Trim();
        }

        var branch = (request.RepositoryBranch ?? "").Trim();
        if (branch.Length == 0)
        {
            branch = BranchFromCi(request.Ci);
        }

        if (branch.Length == 0)
        {
            branch = NormalizeBranch(request.SourceBranchName);
        }

        if (branch.Length == 0)
        {
            branch = NormalizeBranch(FirstRootBranch(request.SourceRoots));
        }

        if (branch.Length == 0 && request.QueriesEnabled && root.Length > 0)
        {
            branch = TryReadBranch(root);
        }

        return new GitValues
        {
            Commit = commit,
            Sha = sha,
            Root = root,
            Url = url,
            Branch = branch,
        };
    }

    private static string ToSha(string? commit)
    {
        var value = (commit ?? "").Trim();
        if (value.Length == 0)
        {
            return "";
        }

        return value.Length <= ShaLength ? value : value.Substring(0, ShaLength);
    }

    private static string NormalizeRoot(string? path)
    {
        var value = (path ?? "").Trim().Replace('\\', '/');
        if (value.Length == 0)
        {
            return "";
        }

        return value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";
    }

    private static string NormalizeBranch(string? value)
    {
        var text = (value ?? "").Trim();
        if (text.Length == 0)
        {
            return "";
        }

        if (text.StartsWith("ref:", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Substring(4).Trim();
        }

        text = text.Replace('\\', '/');
        const string heads = "refs/heads/";
        const string tags = "refs/tags/";
        if (text.StartsWith(heads, StringComparison.Ordinal))
        {
            text = text.Substring(heads.Length);
        }
        else if (text.StartsWith(tags, StringComparison.Ordinal))
        {
            text = text.Substring(tags.Length);
        }

        return text.Trim();
    }

    private static string BranchFromCi(IReadOnlyDictionary<string, string?>? values)
    {
        if (values is null)
        {
            return "";
        }

        string? Get(string key)
        {
            if (!CiKeySet.Contains(key))
            {
                return null;
            }

            return values.TryGetValue(key, out var value) ? value : null;
        }

        bool Has(string key)
        {
            return !string.IsNullOrWhiteSpace(Get(key));
        }

        if (TryPullRequestNumber(Get("GITHUB_REF"), out var number))
        {
            return "pr" + number;
        }

        if (Has("GITHUB_REF"))
        {
            return NormalizeBranch(Get("GITHUB_REF"));
        }

        if (Has("SYSTEM_PULLREQUEST_PULLREQUESTID"))
        {
            return "pr" + Get("SYSTEM_PULLREQUEST_PULLREQUESTID")!.Trim();
        }

        if (TryPullRequestNumber(Get("BUILD_SOURCEBRANCH"), out number))
        {
            return "pr" + number;
        }

        if (Has("BUILD_SOURCEBRANCH"))
        {
            return NormalizeBranch(Get("BUILD_SOURCEBRANCH"));
        }

        if (Has("APPVEYOR_PULL_REQUEST_NUMBER"))
        {
            return "pr" + Get("APPVEYOR_PULL_REQUEST_NUMBER")!.Trim();
        }

        if (Has("APPVEYOR_REPO_TAG_NAME"))
        {
            return Get("APPVEYOR_REPO_TAG_NAME")!.Trim();
        }

        if (Has("APPVEYOR_REPO_BRANCH"))
        {
            return Get("APPVEYOR_REPO_BRANCH")!.Trim();
        }

        if (Has("TEAMCITY_BUILD_BRANCH"))
        {
            return Get("TEAMCITY_BUILD_BRANCH")!.Trim();
        }

        var travisPr = Get("TRAVIS_PULL_REQUEST");
        if (!string.IsNullOrWhiteSpace(travisPr) && !travisPr!.Trim().Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return "pr" + travisPr.Trim();
        }

        if (Has("TRAVIS_BRANCH"))
        {
            return Get("TRAVIS_BRANCH")!.Trim();
        }

        if (Has("CIRCLE_PR_NUMBER"))
        {
            return "pr" + Get("CIRCLE_PR_NUMBER")!.Trim();
        }

        if (Has("CIRCLE_TAG"))
        {
            return Get("CIRCLE_TAG")!.Trim();
        }

        if (Has("CIRCLE_BRANCH"))
        {
            return Get("CIRCLE_BRANCH")!.Trim();
        }

        if (Has("CI_COMMIT_TAG"))
        {
            return Get("CI_COMMIT_TAG")!.Trim();
        }

        if (Has("CI_MERGE_REQUEST_IID"))
        {
            return "pr" + Get("CI_MERGE_REQUEST_IID")!.Trim();
        }

        if (Has("CI_EXTERNAL_PULL_REQUEST_IID"))
        {
            return "pr" + Get("CI_EXTERNAL_PULL_REQUEST_IID")!.Trim();
        }

        if (Has("CI_COMMIT_BRANCH"))
        {
            return Get("CI_COMMIT_BRANCH")!.Trim();
        }

        if (Has("BUDDY_EXECUTION_PULL_REQUEST_NO"))
        {
            return "pr" + Get("BUDDY_EXECUTION_PULL_REQUEST_NO")!.Trim();
        }

        if (Has("BUDDY_EXECUTION_TAG"))
        {
            return Get("BUDDY_EXECUTION_TAG")!.Trim();
        }

        if (Has("BUDDY_EXECUTION_BRANCH"))
        {
            return Get("BUDDY_EXECUTION_BRANCH")!.Trim();
        }

        if (Has("CHANGE_ID"))
        {
            return "pr" + Get("CHANGE_ID")!.Trim();
        }

        if (Has("GIT_LOCAL_BRANCH"))
        {
            return Get("GIT_LOCAL_BRANCH")!.Trim();
        }

        return "";
    }

    private static string TryReadBranch(string root)
    {
        var head = root + ".git/HEAD";
        if (File.Exists(head))
        {
            return NormalizeBranch(File.ReadAllText(head));
        }

        var gitFile = root.TrimEnd('/') + "/.git";
        if (!File.Exists(gitFile))
        {
            return "";
        }

        var text = File.ReadAllText(gitFile).Trim();
        const string prefix = "gitdir:";
        if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }

        var gitDir = text.Substring(prefix.Length).Trim();
        if (gitDir.Length == 0)
        {
            return "";
        }

        if (!Path.IsPathRooted(gitDir))
        {
            var nativeRoot = root.TrimEnd('/').Replace('/', Path.DirectorySeparatorChar);
            gitDir = Path.GetFullPath(Path.Combine(nativeRoot, gitDir.Replace('/', Path.DirectorySeparatorChar)));
        }

        var worktreeHead = Path.Combine(gitDir, "HEAD");
        if (!File.Exists(worktreeHead))
        {
            return "";
        }

        return NormalizeBranch(File.ReadAllText(worktreeHead));
    }

    private static bool TryPullRequestNumber(string? reference, out string number)
    {
        number = "";
        var text = (reference ?? "").Trim().Replace('\\', '/');
        const string marker = "refs/pull/";
        var index = text.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            return false;
        }

        var rest = text.Substring(index + marker.Length);
        var slash = rest.IndexOf('/');
        number = (slash >= 0 ? rest.Substring(0, slash) : rest).Trim();
        return number.Length > 0;
    }

    private static string FirstGitRoot(IReadOnlyList<GitSourceRoot>? roots)
    {
        if (roots is null)
        {
            return "";
        }

        foreach (var root in roots)
        {
            if (!string.Equals(root.SourceControl, "git", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(root.NestedRoot))
            {
                continue;
            }

            var normalized = NormalizeRoot(root.Path);
            if (normalized.Length > 0)
            {
                return normalized;
            }
        }

        return "";
    }

    private static string FirstRootBranch(IReadOnlyList<GitSourceRoot>? roots)
    {
        if (roots is null)
        {
            return "";
        }

        foreach (var root in roots)
        {
            if (!string.Equals(root.SourceControl, "git", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(root.NestedRoot))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(root.BranchName))
            {
                return root.BranchName;
            }
        }

        return "";
    }

    private static string First(string? preferred, string? fallback)
    {
        var value = (preferred ?? "").Trim();
        return value.Length > 0 ? value : (fallback ?? "").Trim();
    }
}
