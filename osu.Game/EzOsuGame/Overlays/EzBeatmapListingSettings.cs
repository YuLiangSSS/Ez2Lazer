// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Settings;

namespace osu.Game.EzOsuGame.Overlays
{
    /// <summary>
    /// 谱面浏览器（在线谱面列表）相关的 Ez 设置。
    /// </summary>
    public partial class EzBeatmapListingSettings : SettingsSubsection
    {
        protected override LocalisableString Header => EzSettingsStrings.EZ_BEATMAP_LISTING_SECTION_HEADER;

        [Resolved(CanBeNull = true)]
        private EzBeatmapBlacklistOverlay? blacklistOverlay { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new SettingsButtonV2
            {
                Text = EzSettingsStrings.BEATMAP_BLACKLIST_VIEW,
                TooltipText = EzSettingsStrings.BEATMAP_BLACKLIST_VIEW_TOOLTIP,
                Keywords = new[] { "blacklist", "black list", "hide", "beatmap", "listing", "黑名单", "谱面", "屏蔽", "拉黑" },
                Action = () => blacklistOverlay?.ShowBlacklist(),
            });
        }
    }
}
