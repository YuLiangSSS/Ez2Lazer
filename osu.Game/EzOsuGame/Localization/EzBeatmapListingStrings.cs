// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.EzOsuGame.Localization
{
    /// <summary>
    /// 谱面浏览器（连接官网谱面服务器、用于下载谱面的页面，即 <see cref="osu.Game.Overlays.BeatmapListingOverlay"/>）的 Ez 扩展字符串。
    /// </summary>
    public static class EzBeatmapListingStrings
    {
        /// <summary>「本地谱面」筛选行的标题。</summary>
        public static readonly LocalisableString LOCAL_FILTER_HEADER = new EzLocalizationManager.EzLocalisableString(
            "本地谱面",
            "Local beatmaps");

        /// <summary>不按本地是否已有谱面过滤（与上游行为一致）。</summary>
        public static readonly LocalisableString LOCAL_FILTER_ANY = new EzLocalizationManager.EzLocalisableString(
            "全部",
            "Any");

        /// <summary>只显示本地已下载的谱面。</summary>
        public static readonly LocalisableString LOCAL_FILTER_OWNED = new EzLocalizationManager.EzLocalisableString(
            "已下载",
            "Downloaded");

        /// <summary>筛掉本地已有的谱面：在线列表只保留尚未下载的卡片。</summary>
        public static readonly LocalisableString LOCAL_FILTER_NOT_OWNED = new EzLocalizationManager.EzLocalisableString(
            "未下载",
            "Not Downloaded");
    }
}
