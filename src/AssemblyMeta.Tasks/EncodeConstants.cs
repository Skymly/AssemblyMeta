using System;
using System.Collections.Generic;
using AssemblyMeta.Core;
using Microsoft.Build.Framework;

namespace AssemblyMeta.Tasks;

/// <summary>把 Constant 与 FileConstant 项编码为生成器可读的 Base64 属性。</summary>
public sealed class EncodeConstants : Microsoft.Build.Utilities.Task
{
    public string? Namespace { get; set; }

    public string? Visibility { get; set; }

    public ITaskItem[] Constants { get; set; } = Array.Empty<ITaskItem>();

    public ITaskItem[] FileConstants { get; set; } = Array.Empty<ITaskItem>();

    [Output]
    public string Payload { get; set; } = "";

    public override bool Execute()
    {
        try
        {
            MemberVisibilityParser.TryParse(Visibility, out var visibility, out var recognized);
            if (!recognized)
            {
                Log.LogWarning("无法识别 AssemblyMetaVisibility“{0}”，已使用 internal。", Visibility);
            }

            var records = new List<ConstantRecord>();
            foreach (var item in Constants ?? Array.Empty<ITaskItem>())
            {
                records.Add(new ConstantRecord(
                    item.ItemSpec ?? "",
                    item.GetMetadata("Value"),
                    item.GetMetadata("Type"),
                    item.GetMetadata("Comment")));
            }

            foreach (var item in FileConstants ?? Array.Empty<ITaskItem>())
            {
                var input = new FileConstantNaming.Input(
                    item.GetMetadata("RelativeDir"),
                    item.GetMetadata("Filename"),
                    item.GetMetadata("Extension"),
                    item.GetMetadata("Link"),
                    item.GetMetadata("Comment"));
                if (FileConstantNaming.TryCreate(input, out var record))
                {
                    records.Add(record);
                }
            }

            Payload = PayloadCodec.EncodeConstants(Namespace, visibility, records);
            return !Log.HasLoggedErrors;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }
}
