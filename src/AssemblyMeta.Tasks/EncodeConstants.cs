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

            var constants = new List<ConstantNameInput>();
            foreach (var item in Constants ?? Array.Empty<ITaskItem>())
            {
                constants.Add(new ConstantNameInput
                {
                    Name = item.ItemSpec ?? "",
                    Value = item.GetMetadata("Value"),
                    Type = item.GetMetadata("Type"),
                    Comment = item.GetMetadata("Comment"),
                });
            }

            var files = new List<FileConstantNameInput>();
            foreach (var item in FileConstants ?? Array.Empty<ITaskItem>())
            {
                files.Add(new FileConstantNameInput
                {
                    RelativeDir = item.GetMetadata("RelativeDir"),
                    Filename = item.GetMetadata("Filename"),
                    Extension = item.GetMetadata("Extension"),
                    Link = item.GetMetadata("Link"),
                    Comment = item.GetMetadata("Comment"),
                });
            }

            var named = MemberNames.Name(new MemberNameRequest
            {
                Area = MemberNameArea.Constants,
                Constants = constants,
                FileConstants = files,
            });
            var records = new List<ConstantRecord>();
            foreach (var member in named.Members)
            {
                records.Add(new ConstantRecord(member.Name, member.Value, member.Type, member.Comment));
            }

            Payload = PayloadCodec.EncodeConstants(Namespace, visibility, records, named.GeneratorDiagnostics);
            return !Log.HasLoggedErrors;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }
}
