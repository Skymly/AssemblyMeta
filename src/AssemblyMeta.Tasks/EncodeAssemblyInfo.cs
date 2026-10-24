using System;
using AssemblyMeta.Core;
using Microsoft.Build.Framework;

namespace AssemblyMeta.Tasks;

/// <summary>把程序集特性值编码为生成器可读的 Base64 属性。</summary>
public sealed class EncodeAssemblyInfo : Microsoft.Build.Utilities.Task
{
    public string? Namespace { get; set; }

    public string? Visibility { get; set; }

    public string? Configuration { get; set; }

    public string? Company { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Product { get; set; }

    public string? Copyright { get; set; }

    public string? Version { get; set; }

    public string? InformationalVersion { get; set; }

    public string? FileVersion { get; set; }

    [Output]
    public string Payload { get; set; } = "";

    public override bool Execute()
    {
        try
        {
            MemberVisibilityParser.TryParse(Visibility, out var visibility, out var recognized);
            if (!recognized)
            {
                Log.LogWarning("Unrecognized AssemblyMetaVisibility “{0}”; fell back to internal. / 无法识别 AssemblyMetaVisibility“{0}”，已使用 internal。", Visibility);
            }

            var model = new AssemblyInfoModel
            {
                Namespace = Namespace,
                Visibility = visibility,
                Configuration = Configuration ?? "",
                Company = Company ?? "",
                Title = Title ?? "",
                Description = Description ?? "",
                Product = Product ?? "",
                Copyright = Copyright ?? "",
                Version = Version ?? "",
                InformationalVersion = InformationalVersion ?? "",
                FileVersion = FileVersion ?? "",
            };
            Payload = PayloadCodec.EncodeAssemblyInfo(model);
            return !Log.HasLoggedErrors;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }
}
