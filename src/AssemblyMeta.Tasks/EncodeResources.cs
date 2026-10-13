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
            var records = new List<ResourceRecord>();
            foreach (var item in Items ?? Array.Empty<ITaskItem>())
            {
                if (TaskSupport.IsFalse(item.GetMetadata("AssemblyMetaResource")))
                {
                    continue;
                }

                var input = TaskSupport.ReadFileInput(item);
                var hasLogical = FileConstantNaming.TryCreate(input, out var named);
                var logical = hasLogical ? named.Value : item.ItemSpec ?? "";
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

                if (string.IsNullOrWhiteSpace(manifest))
                {
                    TaskSupport.LogCodeWarning(this, "AM0009", item.ItemSpec, "嵌入资源“{0}”没有清单名，已跳过。", item.ItemSpec ?? "");
                    continue;
                }

                if (!ResourceNaming.TryCreate(input, manifest, out var record))
                {
                    TaskSupport.LogCodeWarning(this, "AM0010", item.ItemSpec, "嵌入资源“{0}”无法映射为标识符，已跳过。", item.ItemSpec ?? "");
                    continue;
                }

                records.Add(record);
            }

            Payload = PayloadCodec.EncodeResources(Namespace, visibility, records);
            return !Log.HasLoggedErrors;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }

}
