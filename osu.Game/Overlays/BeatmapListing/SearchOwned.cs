// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Localization;

namespace osu.Game.Overlays.BeatmapListing
{
    /// <summary>
    /// 谱面浏览器（在线谱面列表）按「本地是否已有该谱面」筛选谱面卡片。
    /// </summary>
    /// <remarks>
    /// 官方搜索接口不支持按本地谱面过滤，因此筛选在客户端完成（见 <see cref="BeatmapListingFilterControl"/>）。
    /// 判定口径与谱面卡片自身的「本地可用」状态一致：本地存在同 <c>OnlineID</c> 且未删除的谱面集
    /// （即卡片右侧显示绿色边条的那些谱面）。
    /// </remarks>
    public enum SearchOwned
    {
        [LocalisableDescription(typeof(EzBeatmapListingStrings), nameof(EzBeatmapListingStrings.LOCAL_FILTER_ANY))]
        Any,

        [LocalisableDescription(typeof(EzBeatmapListingStrings), nameof(EzBeatmapListingStrings.LOCAL_FILTER_OWNED))]
        Owned,

        [LocalisableDescription(typeof(EzBeatmapListingStrings), nameof(EzBeatmapListingStrings.LOCAL_FILTER_NOT_OWNED))]
        NotOwned,
    }
}
