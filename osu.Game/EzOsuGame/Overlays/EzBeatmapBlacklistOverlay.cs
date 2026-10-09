// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Beatmaps;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.EzOsuGame.Overlays
{
    /// <summary>
    /// 谱面浏览器黑名单的查看 / 管理界面（设置 → Ez 谱面浏览器 → 查看黑名单谱面）。
    /// </summary>
    /// <remarks>
    /// 数据直接来自 <see cref="EzBeatmapBlacklistStore"/>，不经过搜索接口，
    /// 因此黑名单里有多少张谱面、它们是否在热门结果里都不影响这里能看到什么。
    /// </remarks>
    public partial class EzBeatmapBlacklistOverlay : OsuFocusedOverlayContainer
    {
        private const float row_height = 40;
        private const float id_column_width = 100;
        private const float action_column_width = 50;

        private const double enter_duration = 500;
        private const double exit_duration = 200;

        /// <summary>
        /// 每次打开时最多补写多少条缺失的标题（旧版本只存了 id）。
        /// </summary>
        private const int title_backfill_limit = 20;

        protected override string PopInSampleName => @"UI/overlay-big-pop-in";
        protected override string PopOutSampleName => @"UI/overlay-big-pop-out";

        private readonly List<BlacklistRow> rows = new List<BlacklistRow>();

        private FillFlowContainer<BlacklistRow> rowFlow = null!;
        private OsuSpriteText countText = null!;
        private OsuSpriteText emptyHint = null!;
        private OsuColour colours = null!;

        [Resolved]
        private EzBeatmapBlacklistStore blacklistStore { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private SettingsOverlay? settings { get; set; }

        public EzBeatmapBlacklistOverlay()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            RelativeSizeAxes = Axes.Both;
            Size = new Vector2(0.6f, 0.72f);

            Masking = true;
            CornerRadius = 10;
        }

        /// <summary>
        /// 打开黑名单界面。
        /// </summary>
        public void ShowBlacklist()
        {
            settings?.Hide();
            Show();
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour loadedColours)
        {
            colours = loadedColours;

            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colours.GreySeaFoamDark,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Horizontal = 16, Vertical = 12 },
                    Child = new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        RowDimensions = new[]
                        {
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(),
                        },
                        Content = new[]
                        {
                            new Drawable[] { createHeader() },
                            new Drawable[] { createTableHeader() },
                            new Drawable[] { createBody() },
                        }
                    }
                },
                new IconButton
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Icon = FontAwesome.Solid.Times,
                    Colour = colours.GreySeaFoamDarker,
                    Scale = new Vector2(0.8f),
                    Margin = new MarginPadding { Top = 10, Right = 10 },
                    Action = Hide,
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            refreshRows();
        }

        protected override void PopIn()
        {
            refreshRows();
            requestMissingTitles();

            this.FadeIn(enter_duration, Easing.OutQuint);
            this.ScaleTo(1, enter_duration, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            this.FadeOut(exit_duration, Easing.OutQuint);
            this.ScaleTo(0.95f, exit_duration, Easing.OutQuint);
        }

        private Drawable createHeader() => new Container
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Padding = new MarginPadding { Bottom = 10 },
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 2),
                Padding = new MarginPadding { Right = 40 },
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = EzBeatmapListingStrings.BLACKLIST_OVERLAY_HEADER,
                        Font = OsuFont.GetFont(size: 26, weight: FontWeight.SemiBold),
                    },
                    countText = new OsuSpriteText
                    {
                        Font = OsuFont.Default.With(size: 14),
                        Colour = colours.GreySeaFoamLighter,
                    },
                    new OsuSpriteText
                    {
                        Text = EzBeatmapListingStrings.BLACKLIST_OVERLAY_DESCRIPTION,
                        Font = OsuFont.Default.With(size: 12),
                        Colour = colours.Gray8,
                    },
                }
            }
        };

        private Drawable createTableHeader() => new Container
        {
            RelativeSizeAxes = Axes.X,
            Height = row_height,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colours.GreySeaFoam,
                },
                createRowGrid(
                    createCell(EzBeatmapListingStrings.BLACKLIST_COLUMN_ID, colours.GrayF, FontWeight.SemiBold),
                    createCell(EzBeatmapListingStrings.BLACKLIST_COLUMN_TITLE, colours.GrayF, FontWeight.SemiBold),
                    createCell(EzBeatmapListingStrings.BLACKLIST_COLUMN_ACTION, colours.GrayF, FontWeight.SemiBold)),
            }
        };

        private Drawable createBody() => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Masking = true,
            CornerRadius = 8,
            Children = new Drawable[]
            {
                new OsuScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = rowFlow = new FillFlowContainer<BlacklistRow>
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 2),
                    }
                },
                emptyHint = new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = EzBeatmapListingStrings.BLACKLIST_OVERLAY_EMPTY,
                    Font = OsuFont.Default.With(size: 16),
                    Colour = colours.Gray7,
                    Alpha = 0,
                },
            }
        };

        private static Drawable createCell(LocalisableString text, Colour4 colour, FontWeight weight) => new OsuSpriteText
        {
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            Text = text,
            Font = OsuFont.GetFont(size: 14, weight: weight),
            Colour = colour,
        };

        private static GridContainer createRowGrid(Drawable idCell, Drawable titleCell, Drawable actionCell) => new GridContainer
        {
            RelativeSizeAxes = Axes.Both,
            Padding = new MarginPadding { Horizontal = 10 },
            ColumnDimensions = new[]
            {
                new Dimension(GridSizeMode.Absolute, id_column_width),
                new Dimension(),
                new Dimension(GridSizeMode.Absolute, action_column_width),
            },
            RowDimensions = new[] { new Dimension() },
            Content = new[] { new Drawable[] { idCell, titleCell, actionCell } },
        };

        /// <summary>
        /// 按存储内容重建整个列表。
        /// </summary>
        private void refreshRows()
        {
            rowFlow.Clear();
            rows.Clear();

            foreach (var entry in blacklistStore.Entries)
                addRow(entry);

            updateRowColours();
            updateState();
        }

        private void addRow(EzBeatmapBlacklistEntry entry)
        {
            var row = new BlacklistRow(entry, colours);
            row.RemoveRequested = () => removeRow(row);

            rows.Add(row);
            rowFlow.Add(row);
        }

        private void removeRow(BlacklistRow row)
        {
            blacklistStore.Remove(row.OnlineId);

            rows.Remove(row);
            rowFlow.Remove(row, true);

            updateRowColours();
            updateState();
        }

        /// <summary>
        /// 让相邻两行一明一暗；删除一行后重新着色，避免出现连续两行同色。
        /// </summary>
        private void updateRowColours()
        {
            for (int i = 0; i < rows.Count; i++)
                rows[i].SetAlternating(i % 2 == 1);
        }

        private void updateState()
        {
            countText.Text = EzBeatmapListingStrings.BLACKLIST_OVERLAY_COUNT.Format(rows.Count);
            emptyHint.Alpha = rows.Count == 0 ? 1 : 0;
        }

        /// <summary>
        /// 旧版本的黑名单只存了谱面 id，这里按 id 取一次标题补写进数据库（失败就保持「标题未知」）。
        /// </summary>
        private void requestMissingTitles()
        {
            if (!api.IsLoggedIn)
                return;

            foreach (var entry in blacklistStore.Entries.Where(e => string.IsNullOrEmpty(e.Title)).Take(title_backfill_limit))
            {
                int onlineId = entry.OnlineId;
                var request = new GetBeatmapSetRequest(onlineId);

                request.Success += beatmapSet =>
                {
                    string title = EzBeatmapBlacklistEntry.DisplayTitleOf(beatmapSet);

                    if (string.IsNullOrEmpty(title))
                        return;

                    blacklistStore.UpdateTitle(onlineId, title);

                    foreach (var row in rows)
                    {
                        if (row.OnlineId != onlineId)
                            continue;

                        row.SetTitle(title);
                        break;
                    }
                };

                api.Queue(request);
            }
        }

        /// <summary>
        /// 黑名单表格中的一行。
        /// </summary>
        private partial class BlacklistRow : CompositeDrawable
        {
            public readonly int OnlineId;

            public Action? RemoveRequested;

            private readonly OsuColour colours;
            private readonly Box background;
            private readonly TruncatingSpriteText titleText;

            public BlacklistRow(EzBeatmapBlacklistEntry entry, OsuColour colours)
            {
                this.colours = colours;
                OnlineId = entry.OnlineId;

                RelativeSizeAxes = Axes.X;
                Height = row_height;

                InternalChildren = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    createRowGrid(
                        new OsuSpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = entry.OnlineId.ToString(CultureInfo.InvariantCulture),
                            Font = OsuFont.Default.With(size: 14),
                            Colour = colours.GrayC,
                        },
                        titleText = new TruncatingSpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            RelativeSizeAxes = Axes.X,
                            Text = string.IsNullOrEmpty(entry.Title) ? EzBeatmapListingStrings.BLACKLIST_OVERLAY_TITLE_UNKNOWN : entry.Title,
                            Font = OsuFont.Default.With(size: 15),
                            Colour = colours.GrayF,
                        },
                        new IconButton
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            Icon = FontAwesome.Solid.Times,
                            IconColour = colours.Gray9,
                            IconHoverColour = colours.Red1,
                            Size = new Vector2(26),
                            TooltipText = EzBeatmapListingStrings.BLACKLIST_BUTTON_REMOVE,
                            Action = () => RemoveRequested?.Invoke(),
                        }),
                };
            }

            public void SetAlternating(bool alternate) => background.Colour = alternate ? colours.GreySeaFoamDarker : colours.GreySeaFoamDark;

            public void SetTitle(string title) => titleText.Text = title;
        }
    }
}
