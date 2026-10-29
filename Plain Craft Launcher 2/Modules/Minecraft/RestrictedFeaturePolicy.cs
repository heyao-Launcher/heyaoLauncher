using PCL.Core.App.Region;
using PCL.Core.App.Localization;
using PCL.Core.UI;

namespace PCL;

internal static class RestrictedFeaturePolicy
{
    // PCL 和姚修改版：上游 PCL CE 会依据 Windows 区域信号，对中国大陆（及无法判定区域）的
    // Minecraft 下载、补全与整合包安装功能做限制。本修改版为个人使用，按需解除该限制。
    // 注意：这意味着由本修改版自行承担与 Minecraft 下载相关的合规责任。
    private const bool DisableRegionRestriction = true;

    internal static RegionAssessment Assess() => RegionPolicy.Evaluate(RegionPolicy.ReadWindowsSignals());

    internal static bool IsRestricted => !DisableRegionRestriction && Assess().IsRestricted;

    internal static bool Check(string feature, bool showHint = true)
    {
        if (DisableRegionRestriction)
        {
            ModBase.Log($"[RegionPolicy] 已解除区域限制，放行功能 {feature}");
            return true;
        }

        var assessment = Assess();
        if (!assessment.IsRestricted) return true;

        ModBase.Log($"[RegionPolicy] 已阻止功能 {feature}，判定原因：{assessment.Reason}");
        if (showHint)
        {
            HintService.Hint(Lang.Text(assessment.Classification == RegionClassification.Unknown
                ? "Region.Feature.Unknown"
                : "Region.Feature.Restricted"), HintType.Error);
        }
        return false;
    }

    internal static void ThrowIfRestricted(string feature)
    {
        if (Check(feature, false)) return;
        throw new ModBase.CancelledException();
    }
}
