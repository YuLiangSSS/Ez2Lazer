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

        /// <summary>「黑名单」筛选行的标题。</summary>
        public static readonly LocalisableString BLACKLIST_FILTER_HEADER = new EzLocalizationManager.EzLocalisableString(
            "黑名单",
            "Blacklist");

        /// <summary>不按黑名单过滤（与上游行为一致）。</summary>
        public static readonly LocalisableString BLACKLIST_FILTER_ANY = new EzLocalizationManager.EzLocalisableString(
            "全部",
            "Any");

        /// <summary>筛掉黑名单中的谱面：在线列表只保留未加入黑名单的卡片。</summary>
        public static readonly LocalisableString BLACKLIST_FILTER_NOT_BLACKLISTED = new EzLocalizationManager.EzLocalisableString(
            "未加入黑名单",
            "Not Blacklisted");

        /// <summary>把谱面卡片加入黑名单的按钮提示。</summary>
        public static readonly LocalisableString BLACKLIST_BUTTON_ADD = new EzLocalizationManager.EzLocalisableString(
            "加入黑名单",
            "Add to blacklist");

        /// <summary>把谱面卡片移出黑名单的按钮提示。</summary>
        public static readonly LocalisableString BLACKLIST_BUTTON_REMOVE = new EzLocalizationManager.EzLocalisableString(
            "移出黑名单",
            "Remove from blacklist");

        /// <summary>黑名单界面标题。</summary>
        public static readonly LocalisableString BLACKLIST_OVERLAY_HEADER = new EzLocalizationManager.EzLocalisableString(
            "谱面黑名单",
            "Beatmap blacklist");

        /// <summary>黑名单界面里的条目数量（{0} = 数量）。</summary>
        public static readonly EzLocalizationManager.EzLocalisableString BLACKLIST_OVERLAY_COUNT = new EzLocalizationManager.EzLocalisableString(
            "共 {0} 张谱面",
            "{0} beatmaps");

        /// <summary>黑名单界面说明。</summary>
        public static readonly LocalisableString BLACKLIST_OVERLAY_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "在谱面浏览器里点谱面卡片右侧的骷髅头即可加入黑名单；这里可以把它们移出黑名单。",
            "Use the skull button on a beatmap card in the beatmap listing to blacklist it; remove them here.");

        /// <summary>黑名单为空时的提示。</summary>
        public static readonly LocalisableString BLACKLIST_OVERLAY_EMPTY = new EzLocalizationManager.EzLocalisableString(
            "黑名单里还没有谱面。",
            "No beatmaps in the blacklist yet.");

        /// <summary>黑名单表格的 ID 列。</summary>
        public static readonly LocalisableString BLACKLIST_COLUMN_ID = new EzLocalizationManager.EzLocalisableString(
            "ID",
            "ID");

        /// <summary>黑名单表格的标题列。</summary>
        public static readonly LocalisableString BLACKLIST_COLUMN_TITLE = new EzLocalizationManager.EzLocalisableString(
            "标题",
            "Title");

        /// <summary>黑名单表格的操作列。</summary>
        public static readonly LocalisableString BLACKLIST_COLUMN_ACTION = new EzLocalizationManager.EzLocalisableString(
            "操作",
            "Action");

        /// <summary>标题未知（旧版本只存了 id，或谱面已不可查询）时的占位文本。</summary>
        public static readonly LocalisableString BLACKLIST_OVERLAY_TITLE_UNKNOWN = new EzLocalizationManager.EzLocalisableString(
            "（标题未知）",
            "(unknown title)");
    }
}
