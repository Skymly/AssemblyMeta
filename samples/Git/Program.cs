using System.Diagnostics;
using Git;

var mode = args.Length > 0 ? args[0] : "real";
var failures = new List<string>();

void Check(string name, string actual, string expected)
{
    if (!string.Equals(actual, expected, StringComparison.Ordinal))
    {
        failures.Add($"{name}: 期望 '{expected}'，实际 '{actual}'");
    }
}

Check("Namespace", typeof(AssemblyMeta).Namespace ?? "", "Git");

if (mode == "pending")
{
    const string pending = "[pending build]";
    Check(nameof(AssemblyMeta.Git.Commit), AssemblyMeta.Git.Commit, pending);
    Check(nameof(AssemblyMeta.Git.Sha), AssemblyMeta.Git.Sha, pending);
    Check(nameof(AssemblyMeta.Git.Root), AssemblyMeta.Git.Root, pending);
    Check(nameof(AssemblyMeta.Git.Url), AssemblyMeta.Git.Url, pending);
    Check(nameof(AssemblyMeta.Git.Branch), AssemblyMeta.Git.Branch, pending);
}
else
{
    var root = AssemblyMeta.Git.Root.TrimEnd('/');
    var commit = Git(root, "rev-parse", "HEAD");
    var branch = Git(root, "rev-parse", "--abbrev-ref", "HEAD");
    var toplevel = Git(root, "rev-parse", "--show-toplevel").Replace('\\', '/').TrimEnd('/') + "/";
    Check(nameof(AssemblyMeta.Git.Commit), AssemblyMeta.Git.Commit, commit);
    Check(nameof(AssemblyMeta.Git.Sha), AssemblyMeta.Git.Sha, commit.Length >= 9 ? commit[..9] : commit);
    Check(nameof(AssemblyMeta.Git.Root), AssemblyMeta.Git.Root, toplevel);
    Check(nameof(AssemblyMeta.Git.Branch), AssemblyMeta.Git.Branch, mode == "pr" ? "pr15" : branch);
    if (mode == "nourl")
    {
        Check(nameof(AssemblyMeta.Git.Url), AssemblyMeta.Git.Url, "");
    }
    else
    {
        Check(nameof(AssemblyMeta.Git.Url), AssemblyMeta.Git.Url, Git(root, "remote", "get-url", "origin"));
    }
}

if (failures.Count > 0)
{
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(failure);
    }

    return 1;
}

Console.WriteLine("AssemblyMeta.Git 样例断言通过。");
return 0;

static string Git(string root, params string[] args)
{
    var start = new ProcessStartInfo(FindGit())
    {
        WorkingDirectory = root,
        RedirectStandardOutput = true,
        UseShellExecute = false,
    };
    foreach (var arg in args)
    {
        start.ArgumentList.Add(arg);
    }

    using var process = Process.Start(start) ?? throw new InvalidOperationException("无法启动 git。");
    var output = process.StandardOutput.ReadToEnd().Trim();
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException("git " + string.Join(' ', args) + " 失败，退出码 " + process.ExitCode);
    }

    return output;
}

static string FindGit()
{
    var path = Environment.GetEnvironmentVariable("PATH") ?? "";
    foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
    {
        var candidate = Path.Combine(dir.Trim(), "git.exe");
        if (File.Exists(candidate))
        {
            return candidate;
        }
    }

    throw new InvalidOperationException("PATH 中找不到 git.exe。");
}
