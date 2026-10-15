using System;
using System.Collections.Generic;
using System.IO;
using AssemblyMeta.Core;
using Microsoft.Build.Framework;

namespace AssemblyMeta.Tasks;

/// <summary>读取中性 resx，把字符串项编码为生成器可读的 Base64 属性。</summary>
public sealed class EncodeStrings : Microsoft.Build.Utilities.Task
{
    public string? Namespace { get; set; }

    public string? Visibility { get; set; }

    public string? ProjectDirectory { get; set; }

    public ITaskItem[] Items { get; set; } = Array.Empty<ITaskItem>();

    [Output]
    public string Payload { get; set; } = "";

    public override bool Execute()
    {
        try
        {
            var visibility = TaskSupport.ParseVisibility(this, Visibility);
            var inputs = new List<StringNameInput>();
            foreach (var item in Items ?? Array.Empty<ITaskItem>())
            {
                if (!ResourceNaming.IsResx(item.GetMetadata("Extension"), item.ItemSpec) || TaskSupport.IsFalse(item.GetMetadata("AssemblyMetaStrings")))
                {
                    continue;
                }

                var input = TaskSupport.ReadFileInput(item);
                if (!FileConstantNaming.TryCreate(input, out var namedPath))
                {
                    inputs.Add(new StringNameInput { OutsideWithoutLink = true, Spec = item.ItemSpec });
                    continue;
                }

                var withCulture = item.GetMetadata("WithCulture");
                if (TaskSupport.IsTrue(withCulture) || (string.IsNullOrEmpty(withCulture) && StringResourceNaming.IsSatellitePath(namedPath.Value)))
                {
                    continue;
                }

                var fullPath = ResolvePath(item.ItemSpec);
                if (!File.Exists(fullPath))
                {
                    TaskSupport.LogCodeError(this, "AM0011", fullPath, "找不到 resx：{0}", fullPath);
                    continue;
                }

                string xml;
                try
                {
                    xml = File.ReadAllText(fullPath);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    TaskSupport.LogCodeError(this, "AM0011", fullPath, "无法读取 resx {0}：{1}", fullPath, ex.Message);
                    continue;
                }

                if (!ResxReader.TryRead(xml, out var entries, out var skipped, out var error))
                {
                    TaskSupport.LogCodeError(this, "AM0011", fullPath, "无法解析 resx {0}：{1}", fullPath, error ?? "");
                    continue;
                }

                foreach (var skippedName in skipped)
                {
                    TaskSupport.LogCodeWarning(this, "AM0007", fullPath, "已跳过 resx 中的非字符串项“{0}”。", skippedName);
                }

                foreach (var entry in entries)
                {
                    inputs.Add(new StringNameInput
                    {
                        LogicalPath = namedPath.Value,
                        Key = entry.Name,
                        Value = entry.Value,
                        Comment = entry.Comment,
                        Spec = fullPath,
                    });
                }
            }

            var named = MemberNames.Name(new MemberNameRequest
            {
                Area = MemberNameArea.Strings,
                Strings = inputs,
            });
            foreach (var diagnostic in named.TaskDiagnostics)
            {
                TaskSupport.LogCodeWarning(this, diagnostic.Id, "", diagnostic.Message);
            }

            var records = new List<ConstantRecord>();
            foreach (var member in named.Members)
            {
                records.Add(new ConstantRecord(member.Name, member.Value, member.Type, member.Comment));
            }

            Payload = PayloadCodec.EncodeStrings(Namespace, visibility, records, named.GeneratorDiagnostics);
            return !Log.HasLoggedErrors;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }

    private string ResolvePath(string? spec)
    {
        var path = spec ?? "";
        if (path.Length == 0 || Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(ProjectDirectory ?? "", path);
    }
}
