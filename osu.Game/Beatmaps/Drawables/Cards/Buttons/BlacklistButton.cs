// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.EzOsuGame.Beatmaps;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Beatmaps.Drawables.Cards.Buttons
{
    /// <summary>
    /// 把谱面卡片加入 / 移出黑名单的按钮（位于收藏按钮与下载（或前往谱面）按钮之间）。
    /// </summary>
    /// <remarks>
    /// 黑名单与是否已下载无关，因此无论谱面是否下载完成，按钮都保持可用。
    /// 点击后不会刷新谱面列表，需要用户重新触发一次筛选（与「本地谱面」筛选的行为一致）。
    /// </remarks>
    public partial class BlacklistButton : BeatmapCardIconButton
    {
        private readonly APIBeatmapSet beatmapSet;

        private bool blacklisted;

        [Resolved(CanBeNull = true)]
        private EzBeatmapBlacklistStore? blacklistStore { get; set; }

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        public BlacklistButton(APIBeatmapSet beatmapSet)
        {
            this.beatmapSet = beatmapSet;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Icon.Icon = FontAwesome.Solid.Skull;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (blacklistStore == null)
            {
                // 没有黑名单存储（例如不依赖 OsuGameBase 的独立测试场景）时隐藏按钮，但保留占位以免卡片布局跳动。
                Enabled.Value = false;
                Alpha = 0;
                return;
            }

            blacklisted = blacklistStore.Contains(beatmapSet.OnlineID);
            Action = toggleBlacklist;

            updateTooltip();
            refreshIconColour();
        }

        /// <summary>
        /// 黑名单状态变化不经过基类的悬停 / 可用性更新路径，需要手动刷新一次图标配色。
        /// </summary>
        private void refreshIconColour()
        {
            bool hovered = IsHovered && Enabled.Value;

            Icon.FadeColour(blacklisted
                ? (hovered ? colours.Red0 : colours.Red1)
                : (hovered ? HoverColour : IdleColour), BeatmapCard.TRANSITION_DURATION, Easing.OutQuint);
        }

        protected override void UpdateIconState(bool isHovered)
        {
            if (blacklisted)
                Icon.FadeColour(isHovered ? colours.Red0 : colours.Red1, BeatmapCard.TRANSITION_DURATION, Easing.OutQuint);
        }

        private void toggleBlacklist()
        {
            if (blacklistStore == null)
                return;

            blacklisted = blacklistStore.Toggle(EzBeatmapBlacklistEntry.FromBeatmapSet(beatmapSet));

            updateTooltip();
            refreshIconColour();
        }

        private void updateTooltip() =>
            TooltipText = blacklisted ? EzBeatmapListingStrings.BLACKLIST_BUTTON_REMOVE : EzBeatmapListingStrings.BLACKLIST_BUTTON_ADD;
    }
}
