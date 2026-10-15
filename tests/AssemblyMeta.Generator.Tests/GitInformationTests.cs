using AssemblyMeta.Core;

namespace AssemblyMeta.Generator.Tests;

public class GitInformationTests
{
    private const string FullCommit = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void CiKeys_are_the_twenty_one_environment_names()
    {
        Assert.Equal(
            new[]
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
            },
            GitInformation.CiKeys);
    }

    [Fact]
    public void Queries_disabled_keep_resolved_properties_and_pending_generated_values()
    {
        var root = Directory.CreateTempSubdirectory("am-git-off");
        try
        {
            Directory.CreateDirectory(Path.Combine(root.FullName, ".git"));
            File.WriteAllText(Path.Combine(root.FullName, ".git", "HEAD"), "ref: refs/heads/from-head\n");

            var kept = GitInformation.Resolve(new GitResolveRequest
            {
                QueriesEnabled = false,
                RepositoryBranch = "kept",
                RepositoryUrl = "https://example.test/already",
                SourceRevisionId = FullCommit,
                PrivateRepositoryUrl = "https://secret.example/repo.git",
                PublishRepositoryUrl = false,
                RepositoryRoot = root.FullName,
                Ci = new Dictionary<string, string?> { ["GITHUB_REF"] = "refs/pull/1/merge" },
            });

            Assert.Equal("kept", kept.Properties.Branch);
            Assert.Equal(FullCommit, kept.Properties.Commit);
            Assert.Equal("aaaaaaaaa", kept.Properties.Sha);
            Assert.Equal("https://example.test/already", kept.Properties.Url);
            Assert.EndsWith("/", kept.Properties.Root);
            Assert.DoesNotContain('\\', kept.Properties.Root);
            AssertPending(kept.Generated);
            Assert.NotEqual("from-head", kept.Properties.Branch);

            var unread = GitInformation.Resolve(new GitResolveRequest
            {
                QueriesEnabled = false,
                SourceRevisionId = FullCommit,
                RepositoryRoot = root.FullName,
            });
            Assert.Equal("", unread.Properties.Branch);
            Assert.Equal(FullCommit, unread.Properties.Commit);
            AssertPending(unread.Generated);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void Queries_enabled_use_the_same_five_strings()
    {
        var resolved = GitInformation.Resolve(new GitResolveRequest
        {
            QueriesEnabled = true,
            SourceRevisionId = "e3be6392f9afe3c1dc3947123f384df570ed9686",
            RepositoryRoot = @"C:\Code\Skymly\AssemblyMeta",
            RepositoryBranch = "main",
            RepositoryUrl = "https://example.test/already",
        });
        AssertSame(resolved);
        Assert.Equal("e3be6392f9afe3c1dc3947123f384df570ed9686", resolved.Properties.Commit);
        Assert.Equal("e3be6392f", resolved.Properties.Sha);
        Assert.Equal("C:/Code/Skymly/AssemblyMeta/", resolved.Properties.Root);
        Assert.Equal("main", resolved.Properties.Branch);
        Assert.False(resolved.Generated.Unavailable);

        var shortCommit = GitInformation.Resolve(new GitResolveRequest
        {
            QueriesEnabled = true,
            SourceRevisionId = "abc",
        });
        AssertSame(shortCommit);
        Assert.Equal("abc", shortCommit.Properties.Sha);

        var presetSha = GitInformation.Resolve(new GitResolveRequest
        {
            QueriesEnabled = true,
            RepositorySha = "deadbeefdeadbeef",
            SourceRevisionId = FullCommit,
        });
        AssertSame(presetSha);
        Assert.Equal("deadbeefdeadbeef", presetSha.Properties.Sha);
    }

    [Fact]
    public void Missing_commit_is_unavailable_and_not_a_placeholder()
    {
        var missing = GitInformation.Resolve(new GitResolveRequest
        {
            QueriesEnabled = true,
            SourceRevisionId = "  ",
            RepositoryBranch = "main",
        });
        AssertSame(missing);
        Assert.Equal("", missing.Generated.Commit);
        Assert.Equal("", missing.Generated.Sha);
        Assert.True(missing.Generated.Unavailable);
        Assert.DoesNotContain(GitInformation.PendingValue, new[]
        {
            missing.Generated.Commit,
            missing.Generated.Sha,
            missing.Generated.Root,
            missing.Generated.Url,
            missing.Generated.Branch,
        });
    }

    [Theory]
    [InlineData("refs/heads/main", "main")]
    [InlineData("ref: refs/heads/feature/foo", "feature/foo")]
    [InlineData("refs/tags/v1.2.3", "v1.2.3")]
    [InlineData("  abcdef  ", "abcdef")]
    public void Branch_refs_are_normalized(string input, string expected)
    {
        var resolved = GitInformation.Resolve(new GitResolveRequest
        {
            QueriesEnabled = true,
            SourceRevisionId = "abc",
            SourceBranchName = input,
        });
        AssertSame(resolved);
        Assert.Equal(expected, resolved.Properties.Branch);
    }

    [Fact]
    public void Ci_pull_requests_become_pr_numbers()
    {
        AssertBranch("pr15", new Dictionary<string, string?>
        {
            ["GITHUB_REF"] = "refs/pull/15/merge",
            ["GIT_LOCAL_BRANCH"] = "feature",
        });
        AssertBranch("pr8", new Dictionary<string, string?>
        {
            ["SYSTEM_PULLREQUEST_PULLREQUESTID"] = "8",
            ["BUILD_SOURCEBRANCH"] = "refs/heads/main",
        });
        AssertBranch("pr3", new Dictionary<string, string?>
        {
            ["CI_MERGE_REQUEST_IID"] = "3",
            ["CI_COMMIT_BRANCH"] = "feature",
        });
        AssertBranch("pr4", new Dictionary<string, string?>
        {
            ["CI_EXTERNAL_PULL_REQUEST_IID"] = "4",
        });
        AssertBranch("pr9", new Dictionary<string, string?>
        {
            ["APPVEYOR_PULL_REQUEST_NUMBER"] = "9",
            ["APPVEYOR_REPO_BRANCH"] = "main",
        });
        AssertBranch("pr2", new Dictionary<string, string?>
        {
            ["TRAVIS_PULL_REQUEST"] = "2",
            ["TRAVIS_BRANCH"] = "main",
        });
        AssertBranch("main", new Dictionary<string, string?>
        {
            ["TRAVIS_PULL_REQUEST"] = "false",
            ["TRAVIS_BRANCH"] = "main",
        });
        AssertBranch("pr7", new Dictionary<string, string?>
        {
            ["CIRCLE_PR_NUMBER"] = "7",
            ["CIRCLE_BRANCH"] = "topic",
        });
        AssertBranch("v1", new Dictionary<string, string?>
        {
            ["CIRCLE_TAG"] = "v1",
            ["CIRCLE_BRANCH"] = "main",
        });
        AssertBranch("release", new Dictionary<string, string?>
        {
            ["TEAMCITY_BUILD_BRANCH"] = "release",
        });
        AssertBranch("pr11", new Dictionary<string, string?>
        {
            ["BUDDY_EXECUTION_PULL_REQUEST_NO"] = "11",
            ["BUDDY_EXECUTION_BRANCH"] = "topic",
        });
        AssertBranch("pr6", new Dictionary<string, string?>
        {
            ["CHANGE_ID"] = "6",
            ["GIT_LOCAL_BRANCH"] = "feature",
        });
        AssertBranch("feature", new Dictionary<string, string?>
        {
            ["GIT_LOCAL_BRANCH"] = "feature",
        });
        AssertBranch("main", new Dictionary<string, string?>
        {
            ["GITHUB_REF"] = "refs/heads/main",
        });
        AssertBranch("v2", new Dictionary<string, string?>
        {
            ["CI_COMMIT_TAG"] = "v2",
            ["CI_COMMIT_BRANCH"] = "main",
        });
        AssertBranch("main", new Dictionary<string, string?>
        {
            ["NOT_A_CI_KEY"] = "refs/pull/99/merge",
            ["GIT_LOCAL_BRANCH"] = "main",
        });
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
        AssertSame(hidden);
        Assert.Equal("", hidden.Properties.Url);
        Assert.False(hidden.Generated.Unavailable);

        var published = GitInformation.Resolve(new GitResolveRequest
        {
            QueriesEnabled = true,
            PrivateRepositoryUrl = "https://github.com/Skymly/AssemblyMeta.git",
            PublishRepositoryUrl = true,
        });
        AssertSame(published);
        Assert.Equal("https://github.com/Skymly/AssemblyMeta.git", published.Properties.Url);
        Assert.True(published.Generated.Unavailable);

        var preset = GitInformation.Resolve(new GitResolveRequest
        {
            QueriesEnabled = true,
            RepositoryUrl = "https://example.test/already",
            PublishRepositoryUrl = false,
            SourceRevisionId = "0123456789abcdef",
        });
        AssertSame(preset);
        Assert.Equal("https://example.test/already", preset.Properties.Url);
    }

    [Fact]
    public void Head_and_worktree_gitdir_supply_branch_when_ci_is_empty()
    {
        var direct = Directory.CreateTempSubdirectory("am-git-head");
        var worktree = Directory.CreateTempSubdirectory("am-git-wt");
        var gitDir = Directory.CreateTempSubdirectory("am-git-dir");
        try
        {
            Directory.CreateDirectory(Path.Combine(direct.FullName, ".git"));
            File.WriteAllText(Path.Combine(direct.FullName, ".git", "HEAD"), "ref: refs/heads/local\n");
            var fromHead = GitInformation.Resolve(new GitResolveRequest
            {
                QueriesEnabled = true,
                SourceRoots = new[] { new GitSourceRoot(direct.FullName, "git", "", "") },
            });
            AssertSame(fromHead);
            Assert.Equal("local", fromHead.Properties.Branch);
            Assert.EndsWith("/", fromHead.Properties.Root);
            Assert.Contains("am-git-head", fromHead.Properties.Root);

            File.WriteAllText(Path.Combine(worktree.FullName, ".git"), "gitdir: " + gitDir.FullName + "\n");
            File.WriteAllText(Path.Combine(gitDir.FullName, "HEAD"), "ref: refs/heads/feature\n");
            var fromWorktree = GitInformation.Resolve(new GitResolveRequest
            {
                QueriesEnabled = true,
                RepositoryRoot = worktree.FullName,
            });
            AssertSame(fromWorktree);
            Assert.Equal("feature", fromWorktree.Properties.Branch);

            var missing = Directory.CreateTempSubdirectory("am-git-none");
            try
            {
                var empty = GitInformation.Resolve(new GitResolveRequest
                {
                    QueriesEnabled = true,
                    RepositoryRoot = missing.FullName,
                });
                Assert.Equal("", empty.Properties.Branch);
                Assert.True(empty.Generated.Unavailable);
            }
            finally
            {
                missing.Delete(recursive: true);
            }
        }
        finally
        {
            direct.Delete(recursive: true);
            worktree.Delete(recursive: true);
            gitDir.Delete(recursive: true);
        }
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
        AssertSame(result);
        Assert.Equal("", result.Properties.Root);
        Assert.Equal("sdk", result.Properties.Branch);
    }

    private static void AssertBranch(string expected, Dictionary<string, string?> ci)
    {
        var resolved = GitInformation.Resolve(new GitResolveRequest
        {
            QueriesEnabled = true,
            SourceRevisionId = "0123456789abcdef",
            Ci = ci,
        });
        AssertSame(resolved);
        Assert.Equal(expected, resolved.Properties.Branch);
    }

    private static void AssertSame(GitResolution resolved)
    {
        Assert.Equal(resolved.Properties.Commit, resolved.Generated.Commit);
        Assert.Equal(resolved.Properties.Sha, resolved.Generated.Sha);
        Assert.Equal(resolved.Properties.Root, resolved.Generated.Root);
        Assert.Equal(resolved.Properties.Url, resolved.Generated.Url);
        Assert.Equal(resolved.Properties.Branch, resolved.Generated.Branch);
    }

    private static void AssertPending(GitGenerated generated)
    {
        Assert.Equal(GitInformation.PendingValue, generated.Commit);
        Assert.Equal(GitInformation.PendingValue, generated.Sha);
        Assert.Equal(GitInformation.PendingValue, generated.Root);
        Assert.Equal(GitInformation.PendingValue, generated.Url);
        Assert.Equal(GitInformation.PendingValue, generated.Branch);
        Assert.False(generated.Unavailable);
    }
}
