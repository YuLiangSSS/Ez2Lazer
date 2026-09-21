// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.Mods;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.CommunityMod
{
    /// <summary>
    /// 开谱时按种子在设定的上下边界之间随机一个 OverallDifficulty。
    /// </summary>
    /// <remarks>
    /// OD 在 <c>IApplicableToDifficulty</c> 阶段写入，早于音符 <c>ApplyDefaults</c> 时用 OD 烘焙判定区间，
    /// 因此所有物件的判定区间都会跟着随机 OD 一起变化；随机值只取决于种子，游玩 / 回放 / 成绩重算一致。
    /// <para>
    /// 上下边界的顺序无关：下边界大于上边界时自动反转，始终在两者的闭区间内随机。
    /// </para>
    /// </remarks>
    public class ManiaModRandomOverallDifficulty : Mod, IApplicableToDifficulty, IHasSeed
    {
        public override string Name => "Random OD";

        public override string Acronym => "ROD";

        public override LocalisableString Description => RandomOverallDifficultyBoundStrings.DESCRIPTION;

        public override IconUsage? Icon => FontAwesome.Solid.Random;

        public override ModType Type => ModType.CommunityMod;

        public override bool Ranked => false;

        public override bool ValidForMultiplayer => true;

        public override bool ValidForFreestyleAsRequiredMod => false;

        /// <summary>
        /// 上下边界的可选范围。
        /// </summary>
        public const double MIN_OD_BOUND = -15;

        public const double MAX_OD_BOUND = 15;

        /// <summary>
        /// 这些 Mod 同样覆写 OD，同时启用时会互相覆盖，因此声明为互斥。
        /// </summary>
        public override Type[] IncompatibleMods => new[]
        {
            typeof(ManiaModAdjust),
            typeof(ManiaModDifficultyAdjust),
        };

        /// <summary>
        /// 随机种子；留空时每次游玩重新掷一次。
        /// </summary>
        [SettingSource(typeof(EzCommonModStrings), nameof(EzCommonModStrings.SEED_LABEL), nameof(EzCommonModStrings.SEED_DESCRIPTION), SettingControlType = typeof(SettingsNumberBox))]
        public Bindable<int?> Seed { get; } = new Bindable<int?>();

        /// <summary>
        /// 随机区间的下边界。
        /// </summary>
        [SettingSource(typeof(RandomOverallDifficultyBoundStrings), nameof(RandomOverallDifficultyBoundStrings.MIN_LABEL), nameof(RandomOverallDifficultyBoundStrings.BOUNDS_DESCRIPTION))]
        public BindableDouble MinOverallDifficulty { get; } = createBound(MIN_OD_BOUND);

        /// <summary>
        /// 随机区间的上边界。
        /// </summary>
        [SettingSource(typeof(RandomOverallDifficultyBoundStrings), nameof(RandomOverallDifficultyBoundStrings.MAX_LABEL), nameof(RandomOverallDifficultyBoundStrings.BOUNDS_DESCRIPTION))]
        public BindableDouble MaxOverallDifficulty { get; } = createBound(MAX_OD_BOUND);

        /// <summary>
        /// 最近一次掷出的 OD。
        /// </summary>
        public double? ActiveOverallDifficulty { get; private set; }

        private static BindableDouble createBound(double defaultValue) => new BindableDouble(defaultValue)
        {
            MinValue = MIN_OD_BOUND,
            MaxValue = MAX_OD_BOUND,
            Precision = 0.1
        };

        public override IEnumerable<(LocalisableString setting, LocalisableString value)> SettingDescription
        {
            get
            {
                yield return (EzCommonModStrings.SEED_LABEL, Seed.Value?.ToString() ?? RandomOverallDifficultyBoundStrings.RANDOM);
                yield return (RandomOverallDifficultyBoundStrings.RANGE_LABEL, $"{getLowerBound():N1} ~ {getUpperBound():N1}");

                if (ActiveOverallDifficulty is double od)
                    yield return (RandomOverallDifficultyBoundStrings.ROLLED_LABEL, $"{od:N1}");
            }
        }

        public void ApplyToDifficulty(BeatmapDifficulty difficulty)
        {
            double randomised = RollOverallDifficulty();

            ActiveOverallDifficulty = randomised;
            difficulty.OverallDifficulty = (float)randomised;
        }

        /// <summary>
        /// 按种子在上下边界之间掷出一个 OD。
        /// </summary>
        /// <remarks>
        /// 边界顺序无关：<see cref="MinOverallDifficulty"/> 大于 <see cref="MaxOverallDifficulty"/> 时自动反转，
        /// 例如下边界填 5、上边界填 2，会在 2~5 之间随机。
        /// </remarks>
        public double RollOverallDifficulty()
        {
            double lower = getLowerBound();
            double upper = getUpperBound();

            if (Precision.AlmostEquals(lower, upper))
                return lower;

            var rng = new Random(EzModSeed.Resolve(Seed));

            return lower + rng.NextDouble() * (upper - lower);
        }

        private double getLowerBound() => Math.Min(MinOverallDifficulty.Value, MaxOverallDifficulty.Value);

        private double getUpperBound() => Math.Max(MinOverallDifficulty.Value, MaxOverallDifficulty.Value);

        public override void ResetSettingsToDefaults()
        {
            base.ResetSettingsToDefaults();

            ActiveOverallDifficulty = null;
        }
    }

    public static class RandomOverallDifficultyBoundStrings
    {
        public static readonly LocalisableString DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "开谱时按种子在设定的 OD 上下边界之间随机 OverallDifficulty",
            "Randomise the OverallDifficulty of a beatmap within the configured bounds on load");

        public static readonly LocalisableString MIN_LABEL = new EzLocalizationManager.EzLocalisableString("下边界", "Lower bound");

        public static readonly LocalisableString MAX_LABEL = new EzLocalizationManager.EzLocalisableString("上边界", "Upper bound");

        public static readonly LocalisableString BOUNDS_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "随机区间的边界，取值范围 -15~15；下边界大于上边界时自动反转",
            "Bounds of the random range, between -15 and 15; the two are swapped when the lower bound is the greater one");

        public static readonly LocalisableString RANGE_LABEL = new EzLocalizationManager.EzLocalisableString("随机区间", "Random range");

        public static readonly LocalisableString ROLLED_LABEL = new EzLocalizationManager.EzLocalisableString("本次 OD", "Rolled OD");

        public static readonly LocalisableString RANDOM = new EzLocalizationManager.EzLocalisableString("随机", "Random");
    }
}
