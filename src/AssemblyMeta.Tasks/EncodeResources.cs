using System;
using System.Collections.Generic;
using AssemblyMeta.Core;
using Microsoft.Build.Framework;

namespace AssemblyMeta.Tasks;

/// <summary>把 EmbeddedResource 项编码为生成器可读的 Base64 属性。不改写原项。</summary>
public sealed class EncodeResources : Microsoft.Build.Utilities.Task
{
    public string? Namespace { get; set; }

    public string? Visibility { get; set; }

    public ITaskItem[] Items { get; set; } = Array.Empty<ITaskItem>();

    [Output]
    public string Payload { get; set; } = "";

    public override bool Execute()
    {
        try
        {
            var visibility = TaskSupport.ParseVisibility(this, Visibility);
            var inputs = new List<ResourceNameInput>();
            foreach (var item in Items ?? Array.Empty<ITaskItem>())
            {
                if (TaskSupport.IsFalse(item.GetMetadata("AssemblyMetaResource")))
                {
                    continue;
                }

                var file = TaskSupport.ReadFileInput(item);
                var hasLogical = FileConstantNaming.TryCreate(file, out var namedPath);
                var logical = hasLogical ? namedPath.Value : item.ItemSpec ?? "";
                var force = TaskSupport.IsTrue(item.GetMetadata("AssemblyMetaResource"));
                if (!force && ResourceNaming.IsResx(item.GetMetadata("Extension"), logical))
                {
                    continue;
                }

                var manifest = item.GetMetadata("ManifestResourceName");
                if (string.IsNullOrWhiteSpace(manifest))
                {
                    manifest = item.GetMetadata("LogicalName");
                }

                inputs.Add(new ResourceNameInput
                {
                    RelativeDir = file.RelativeDir,
                    Filename = file.FileName,
                    Extension = file.Extension,
                    Link = file.Link,
                    Comment = file.Comment,
                    ManifestName = manifest,
                    Spec = item.ItemSpec,
                });
            }

            var named = MemberNames.Name(new MemberNameRequest
            {
                Area = MemberNameArea.Resources,
                Resources = inputs,
            });
            foreach (var diagnostic in named.TaskDiagnostics)
            {
                TaskSupport.LogCodeWarning(this, diagnostic.Id, diagnostic.File, diagnostic.Message);
            }

            var records = new List<ResourceRecord>();
            foreach (var member in named.Members)
            {
                records.Add(new ResourceRecord(member.Name, member.ManifestName ?? "", member.Comment));
            }

            Payload = PayloadCodec.EncodeResources(Namespace, visibility, records, named.GeneratorDiagnostics);
            return !Log.HasLoggedErrors;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }

}
