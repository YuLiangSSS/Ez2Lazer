// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Utils;

namespace osu.Game.Screens.Play.Break
{
    public partial class BreakInfoLine<T> : Container
        where T : struct
    {
        private const int margin = 2;

        public Bindable<T> Current = new Bindable<T>();

        private readonly LocalisableString name;

        private OsuSpriteText valueText = null!;

        public BreakInfoLine(LocalisableString name)
        {
            this.name = name;

            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            Children = new Drawable[]
            {
                new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.CentreRight,
                    Text = name,
                    Font = OsuFont.GetFont(size: 17),
                    Margin = new MarginPadding { Right = margin },
                    Colour = colours.Yellow,
                },
                valueText = new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.CentreLeft,
                    Text = @"-",
                    Font = OsuFont.GetFont(weight: FontWeight.Bold, size: 17),
                    Margin = new MarginPadding { Left = margin },
                    Colour = colours.YellowLight,
                }
            };

            Current.BindValueChanged(text => valueText.Text = Format(text.NewValue));
        }

        protected virtual LocalisableString Format(T count)
        {
            if (count is Enum countEnum)
                return countEnum.GetLocalisableDescription();

            return count.ToString() ?? string.Empty;
        }
    }

    public partial class PercentageBreakInfoLine : BreakInfoLine<double>
    {
        // 必须强引用持有：GetBindable 返回弱绑定副本，若不加字段持有会被 GC，开关切换时订阅将失效。
        private IBindable<bool> hideScores = null!;

        public PercentageBreakInfoLine(LocalisableString name)
            : base(name)
        {
        }

        [BackgroundDependencyLoader]
        private void load(Ez2ConfigManager ezConfig)
        {
            hideScores = ezConfig.GetBindable<bool>(Ez2Setting.HideScores);
            hideScores.BindValueChanged(_ => Current.TriggerChange());
        }

        protected override LocalisableString Format(double count)
        {
            string formatted = count.FormatAccuracy().ToString();
            return hideScores?.Value == true ? EzScoreVisibility.Mask(formatted) : formatted;
        }
    }
}
