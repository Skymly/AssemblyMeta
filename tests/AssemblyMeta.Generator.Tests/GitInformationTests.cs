using AssemblyMeta.Core;

namespace AssemblyMeta.Generator.Tests;

public class GitInformationTests
{
    [Fact]
    public void Sha_is_first_nine_or_full_when_shorter()
    {
        Assert.Equal("e3be6392f", GitInformation.ToSha("e3be6392f9afe3c1dc3947123f384df570ed9686"));
        Assert.Equal("abc", GitInformation.ToSha("abc"));
        Assert.Equal("", GitInformation.ToSha("  "));
    }

    [Fact]
    public void Root_uses_forward_slashes_and_trailing_slash()
    {
        Assert.Equal("C:/Code/Skymly/AssemblyMeta/", GitInformation.NormalizeRoot(@"C:\Code\Skymly\AssemblyMeta\"));
        Assert.Equal("C:/Code/Skymly/AssemblyMeta/", GitInformation.NormalizeRoot("C:/Code/Skymly/AssemblyMeta"));
    }

    [Theory]
    [InlineData("refs/heads/main", "main")]
    [InlineData("ref: refs/heads/feature/foo", "feature/foo")]
    [InlineData("refs/tags/v1.2.3", "v1.2.3")]
    [InlineData("  abcdef  ", "abcdef")]
    public void Branch_refs_are_normalized(string input, string expected)
    {
        Assert.Equal(expected, GitInformation.NormalizeBranch(input));
    }

    [Fact]
    public void Ci_pull_requests_become_pr_numbers()
    {
        Assert.Equal("pr15", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["GITHUB_REF"] = "refs/pull/15/merge",
            ["GIT_LOCAL_BRANCH"] = "feature",
        }));
        Assert.Equal("pr8", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["SYSTEM_PULLREQUEST_PULLREQUESTID"] = "8",
            ["BUILD_SOURCEBRANCH"] = "refs/pull/8/merge",
        }));
        Assert.Equal("pr3", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["CI_MERGE_REQUEST_IID"] = "3",
            ["CI_COMMIT_BRANCH"] = "feature",
        }));
        Assert.Equal("pr4", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["CI_EXTERNAL_PULL_REQUEST_IID"] = "4",
        }));
        Assert.Equal("pr9", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["APPVEYOR_PULL_REQUEST_NUMBER"] = "9",
            ["APPVEYOR_REPO_BRANCH"] = "main",
        }));
        Assert.Equal("pr2", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["TRAVIS_PULL_REQUEST"] = "2",
            ["TRAVIS_BRANCH"] = "main",
        }));
        Assert.Equal("main", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["TRAVIS_PULL_REQUEST"] = "false",
            ["TRAVIS_BRANCH"] = "main",
        }));
        Assert.Equal("pr7", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["CIRCLE_PR_NUMBER"] = "7",
            ["CIRCLE_BRANCH"] = "topic",
        }));
        Assert.Equal("v1", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["CIRCLE_TAG"] = "v1",
            ["CIRCLE_BRANCH"] = "main",
        }));
        Assert.Equal("release", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["TEAMCITY_BUILD_BRANCH"] = "release",
        }));
        Assert.Equal("pr11", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["BUDDY_EXECUTION_PULL_REQUEST_NO"] = "11",
            ["BUDDY_EXECUTION_BRANCH"] = "topic",
        }));
        Assert.Equal("pr6", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["CHANGE_ID"] = "6",
            ["GIT_LOCAL_BRANCH"] = "feature",
        }));
        Assert.Equal("feature", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["GIT_LOCAL_BRANCH"] = "feature",
        }));
        Assert.Equal("main", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["GITHUB_REF"] = "refs/heads/main",
        }));
        Assert.Equal("v2", GitInformation.BranchFromCi(new Dictionary<string, string?>
        {
            ["CI_COMMIT_TAG"] = "v2",
            ["CI_COMMIT_BRANCH"] = "main",
        }));
    }

    [Fact]
    public void User_branch_wins_over_ci_and_head_is_not_read_when_queries_disabled()
    {
        var reads = 0;
        var result = GitInformation.Resolve(new GitResolveRequest
        {
            QueriesEnabled = false,
            RepositoryBranch = "kept",
            SourceRevisionId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            PrivateRepositoryUrl = "https://secret.example/repo.git",
            PublishRepositoryUrl = false,
            Ci = new Dictionary<string, string?> { ["GITHUB_REF"] = "refs/pull/1/merge" },
            SourceRoots = new[]
            {
                new GitSourceRoot(@"C:\repo\", "git", "", "refs/heads/main"),
                new GitSourceRoot(@"C:\repo\sub\", "git", "sub", "refs/heads/sub"),
            },
            FileExists = _ => { reads++; return true; },
            ReadFile = _ => { reads++; return "ref: refs/heads/other"; },
        });

        Assert.Equal("kept", result.Branch);
        Assert.Equal("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", result.Commit);
        Assert.Equal("aaaaaaaaa", result.Sha);
        Assert.Equal("C:/repo/", result.Root);
        Assert.Equal("", result.Url);
        Assert.Equal(0, reads);
        Assert.False(result.Unavailable);
    }

    [Fact]
    public void Publish_flag_exposes_private_url_and_missing_commit_is_unavailable()
    {
        var hidden = GitInformation.Resolve(new GitResolveRequest
        {
            QueriesEnabled = true,
            SourceRevisionId = "0123456789abcdef",
            PrivateRepositoryUrl = "https://github.com/Skymly/AssemblyMeta.git",
            PublishRepositoryUrl = false,
        });
        Assert.Equal("", hidden.Url);
        Assert.False(hidden.Unavailable);

        var published = GitInformation.Resolve(new GitResolveRequest
        {
            QueriesEnabled = true,
            PrivateRepositoryUrl = "https://github.com/Skymly/AssemblyMeta.git",
            PublishRepositoryUrl = true,
        });
        Assert.Equal("https://github.com/Skymly/AssemblyMeta.git", published.Url);
        Assert.True(published.Unavailable);

        var preset = GitInformation.Resolve(new GitResolveRequest
        {
            QueriesEnabled = true,
            RepositoryUrl = "https://example.test/already",
            PublishRepositoryUrl = false,
            SourceRevisionId = "0123456789abcdef",
        });
        Assert.Equal("https://example.test/already", preset.Url);
    }

    [Fact]
    public void Head_and_worktree_gitdir_supply_branch_when_ci_is_empty()
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [@"C:/repo/.git/HEAD"] = "ref: refs/heads/local\n",
        };
        var direct = GitInformation.Resolve(new GitResolveRequest
        {
            QueriesEnabled = true,
            SourceRoots = new[] { new GitSourceRoot(@"C:\repo\", "git", "", "") },
            FileExists = path => files.ContainsKey(path.Replace('\\', '/')),
            ReadFile = path => files[path.Replace('\\', '/')],
        });
        Assert.Equal("local", direct.Branch);
        Assert.Equal("C:/repo/", direct.Root);

        var worktree = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [@"C:/wt/.git"] = "gitdir: C:/git/worktrees/feature\n",
            [@"C:/git/worktrees/feature/HEAD"] = "ref: refs/heads/feature\n",
        };
        var fromWorktree = GitInformation.TryReadBranch(
            "C:/wt/",
            path => worktree.ContainsKey(path.Replace('\\', '/')),
            path => worktree[path.Replace('\\', '/')]);
        Assert.Equal("feature", fromWorktree);
    }

    [Fact]
    public void Nested_source_root_is_ignored()
    {
        var result = GitInformation.Resolve(new GitResolveRequest
        {
            QueriesEnabled = true,
            SourceRoots = new[]
            {
                new GitSourceRoot(@"C:\repo\modules\sub\", "git", "modules/sub", "refs/heads/sub"),
                new GitSourceRoot(@"D:\other\", "tfvc", "", "refs/heads/nope"),
            },
            SourceBranchName = "refs/heads/sdk",
        });
        Assert.Equal("", result.Root);
        Assert.Equal("sdk", result.Branch);
    }
}
