// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Localization;

namespace osu.Game.Overlays.BeatmapListing
{
    /// <summary>
    /// 谱面浏览器（在线谱面列表）按本地黑名单筛选谱面卡片。
    /// </summary>
    /// <remarks>
    /// 官方搜索接口不支持按本地黑名单过滤，因此筛选在客户端完成（见 <see cref="BeatmapListingFilterControl"/>）。
    /// 黑名单保存在 <see cref="osu.Game.EzOsuGame.Beatmaps.EzBeatmapBlacklistStore"/>，
    /// 判定口径为谱面集的 <c>OnlineID</c> 是否存在于该列表。
    /// 想查看/管理黑名单内容，请使用设置里的「查看黑名单谱面」。
    /// </remarks>
    public enum SearchBlacklist
    {
        [LocalisableDescription(typeof(EzBeatmapListingStrings), nameof(EzBeatmapListingStrings.BLACKLIST_FILTER_ANY))]
        Any,

        [LocalisableDescription(typeof(EzBeatmapListingStrings), nameof(EzBeatmapListingStrings.BLACKLIST_FILTER_NOT_BLACKLISTED))]
        NotBlacklisted,
    }
}
