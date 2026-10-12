using System;
using System.Collections.Generic;
using AssemblyMeta.Core;
using Microsoft.Build.Framework;

namespace AssemblyMeta.Tasks;

/// <summary>把 AssemblyMetadata 项编码为生成器可读的 Base64 属性。不改写原项。</summary>
public sealed class EncodeMetadata : Microsoft.Build.Utilities.Task
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
            var records = new List<ConstantRecord>();
            foreach (var item in Items ?? Array.Empty<ITaskItem>())
            {
                var name = item.ItemSpec ?? "";
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                records.Add(new ConstantRecord(name, item.GetMetadata("Value"), "string", item.GetMetadata("Comment")));
            }

            Payload = PayloadCodec.EncodeMetadata(Namespace, visibility, records);
            return !Log.HasLoggedErrors;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }
}
