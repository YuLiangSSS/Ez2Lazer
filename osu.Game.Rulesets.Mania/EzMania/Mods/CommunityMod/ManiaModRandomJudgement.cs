// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.Mods;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Scoring;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.CommunityMod
{
    /// <summary>
    /// 按种子随机每个判定的判定区间，以及 LN 面尾的判定倍数。
    /// </summary>
    /// <remarks>
    /// 判定的基准值与取值范围同 <see cref="ManiaModAdjust"/> 的「自定义打击范围」（Lazer 的六个默认判定区间），
    /// 随机结果通过 <see cref="ManiaHitWindows.SetModOverride"/> 与 <see cref="TailNote.RELEASE_WINDOW_LENIENCE"/>
    /// 全局生效，因此游玩、回放、Session 重判与选歌界面显示的区间完全一致。
    /// </remarks>
    public class ManiaModRandomJudgement : Mod, IApplicableToDifficulty, IManiaHitRangeProvider, IHasSeed
    {
        public override string Name => "Random Judgement";

        public override string Acronym => "RJ";

        public override LocalisableString Description => RandomJudgementStrings.DESCRIPTION;

        public override IconUsage? Icon => FontAwesome.Solid.Random;

        public override ModType Type => ModType.CommunityMod;

        public override bool Ranked => false;

        public override bool ValidForMultiplayer => true;

        public override bool ValidForFreestyleAsRequiredMod => false;

        /// <summary>
        /// 这些 Mod 同样通过 <see cref="ManiaHitWindows.SetModOverride"/> 覆写判定区间（该覆写是单一全局槽位），
        /// 同时启用时会互相覆盖，因此声明为互斥。
        /// </summary>
        public override Type[] IncompatibleMods => new[]
        {
            typeof(ManiaModAdjust),
            typeof(ManiaModJudgmentsAdjust),
            typeof(ManiaModNewJudgement),
        };

        /// <summary>Lazer 面尾判定默认倍数（与 <see cref="TailNote.RELEASE_WINDOW_LENIENCE"/> 的默认值一致）。</summary>
        public const double DEFAULT_TAIL_LENIENCE = 1.5D;

        /// <summary>
        /// 随机种子；留空时每次游玩重新掷一次。
        /// </summary>
        [SettingSource(typeof(EzCommonModStrings), nameof(EzCommonModStrings.SEED_LABEL), nameof(EzCommonModStrings.SEED_DESCRIPTION), SettingControlType = typeof(SettingsNumberBox))]
        public Bindable<int?> Seed { get; } = new Bindable<int?>();

        #region 随机判定区间

        [SettingSource(typeof(RandomJudgementStrings), nameof(RandomJudgementStrings.PERFECT_LABEL), nameof(RandomJudgementStrings.RANGE_DESCRIPTION))]
        public BindableDouble PerfectRange { get; } = createRange(22.4D);

        [SettingSource(typeof(RandomJudgementStrings), nameof(RandomJudgementStrings.GREAT_LABEL), nameof(RandomJudgementStrings.RANGE_DESCRIPTION))]
        public BindableDouble GreatRange { get; } = createRange(64);

        [SettingSource(typeof(RandomJudgementStrings), nameof(RandomJudgementStrings.GOOD_LABEL), nameof(RandomJudgementStrings.RANGE_DESCRIPTION))]
        public BindableDouble GoodRange { get; } = createRange(97);

        [SettingSource(typeof(RandomJudgementStrings), nameof(RandomJudgementStrings.OK_LABEL), nameof(RandomJudgementStrings.RANGE_DESCRIPTION))]
        public BindableDouble OkRange { get; } = createRange(127);

        [SettingSource(typeof(RandomJudgementStrings), nameof(RandomJudgementStrings.MEH_LABEL), nameof(RandomJudgementStrings.RANGE_DESCRIPTION))]
        public BindableDouble MehRange { get; } = createRange(151);

        [SettingSource(typeof(RandomJudgementStrings), nameof(RandomJudgementStrings.MISS_LABEL), nameof(RandomJudgementStrings.RANGE_DESCRIPTION))]
        public BindableDouble MissRange { get; } = createRange(188);

        #endregion

        #region LN 面尾判定倍数

        [SettingSource(typeof(RandomJudgementStrings), nameof(RandomJudgementStrings.TAIL_LENIENCE_LABEL), nameof(RandomJudgementStrings.TAIL_LENIENCE_DESCRIPTION))]
        public BindableDouble TailLenienceAmplitude { get; } = new BindableDouble
        {
            MinValue = 0,
            MaxValue = 100,
            Precision = 1
        };

        #endregion

        /// <summary>
        /// 当前生效的随机判定区间；所有判定区间都没有设置随机时为 <c>null</c>。
        /// </summary>
        public ManiaModifyHitRange? ActiveRange { get; private set; }

        /// <summary>
        /// 当前生效的 LN 面尾判定倍数。
        /// </summary>
        public double ActiveTailLenience { get; private set; } = DEFAULT_TAIL_LENIENCE;

        private static BindableDouble createRange(double defaultValue) => new BindableDouble(defaultValue)
        {
            Precision = 0.1,
            MinValue = 0,
            MaxValue = 250
        };

        public override IEnumerable<(LocalisableString setting, LocalisableString value)> SettingDescription
        {
            get
            {
                yield return (EzCommonModStrings.SEED_LABEL, Seed.Value?.ToString() ?? RandomJudgementStrings.RANDOM);
                yield return (RandomJudgementStrings.PERFECT_LABEL, getRangeDescription(PerfectRange.Value));
                yield return (RandomJudgementStrings.GREAT_LABEL, getRangeDescription(GreatRange.Value));
                yield return (RandomJudgementStrings.GOOD_LABEL, getRangeDescription(GoodRange.Value));
                yield return (RandomJudgementStrings.OK_LABEL, getRangeDescription(OkRange.Value));
                yield return (RandomJudgementStrings.MEH_LABEL, getRangeDescription(MehRange.Value));
                yield return (RandomJudgementStrings.MISS_LABEL, getRangeDescription(MissRange.Value));
                yield return (RandomJudgementStrings.TAIL_LENIENCE_LABEL, $"{TailLenienceAmplitude.Value:N1} %");

                if (ActiveRange is { } range)
                {
                    yield return (RandomJudgementStrings.ROLLED_LABEL,
                        $"P {range.Perfect:0.#} / G {range.Great:0.#} / Gd {range.Good:0.#} / Ok {range.Ok:0.#} / M {range.Meh:0.#} / Miss {range.Miss:0.#} ms");

                    yield return (RandomJudgementStrings.TAIL_LENIENCE_ROLLED_LABEL, $"{ActiveTailLenience:N2}x");
                }
            }
        }

        public void ApplyToDifficulty(BeatmapDifficulty difficulty)
        {
            ManiaModifyHitRange? range = ComputeRange(out double tailLenience);

            ActiveRange = range;
            ActiveTailLenience = tailLenience;
            TailNote.RELEASE_WINDOW_LENIENCE = tailLenience;

            if (range is { } value)
                ManiaHitWindows.SetModOverride(value);
            else
                ManiaHitWindows.ClearModOverride();
        }

        /// <summary>
        /// 谱面属性面板 / 选歌界面显示用的随机结果。
        /// </summary>
        public ManiaModifyHitRange? GetDisplayHitRange(IBeatmapInfo beatmapInfo) => ComputeRange(out _);

        /// <summary>
        /// 按种子掷出六个判定区间的随机值以及 LN 面尾倍数。
        /// </summary>
        /// <param name="tailLenience">掷出的 LN 面尾判定倍数。</param>
        /// <returns>需要覆写判定区间时返回随机结果；六个判定区间都是 0（不随机）时返回 <c>null</c>。</returns>
        private ManiaModifyHitRange? ComputeRange(out double tailLenience)
        {
            tailLenience = DEFAULT_TAIL_LENIENCE;

            double[] ranges =
            [
                PerfectRange.Value,
                GreatRange.Value,
                GoodRange.Value,
                OkRange.Value,
                MehRange.Value,
                MissRange.Value,
            ];

            bool anyWindowRandom = false;

            foreach (double range in ranges)
                anyWindowRandom |= range > 0;

            // 只有面尾参与随机时不需要覆写判定区间。
            bool anyTailRandom = TailLenienceAmplitude.Value > 0;

            if (!anyWindowRandom && !anyTailRandom)
                return null;

            var rng = new Random(EzModSeed.Resolve(Seed));

            // 掷值顺序固定，保证同一 seed 下各判定类型的结果稳定、可复现。
            double perfect = nextRange(rng, PerfectRange.Value);
            double great = nextRange(rng, GreatRange.Value);
            double good = nextRange(rng, GoodRange.Value);
            double ok = nextRange(rng, OkRange.Value);
            double meh = nextRange(rng, MehRange.Value);
            double miss = nextRange(rng, MissRange.Value);

            tailLenience = nextWindow(rng, DEFAULT_TAIL_LENIENCE, TailLenienceAmplitude.Value);

            if (!anyWindowRandom)
                return null;

            // 判定依赖区间的单调性（Perfect ⊂ Great ⊂ ... ⊂ Miss），随机后必须保持，
            // 否则会出现「落在更大的区间里却判定成更差的等级」。
            const double minimum_gap = 1;

            perfect = Math.Max(1, perfect);
            great = Math.Max(perfect + minimum_gap, great);
            good = Math.Max(great + minimum_gap, good);
            ok = Math.Max(good + minimum_gap, ok);
            meh = Math.Max(ok + minimum_gap, meh);
            miss = Math.Max(meh + minimum_gap, miss);

            return new ManiaModifyHitRange(perfect, great, good, ok, meh, miss);
        }

        /// <summary>
        /// 判定区间的随机：以设定值为中心，在 [设定值 / 2, 设定值 × 1.5] 内掷出一个随机区间。
        /// </summary>
        /// <remarks>
        /// 判定的数值口径与 <see cref="ManiaModAdjust"/> 的「自定义打击范围」一致（同一个 Lazer 默认值与取值范围），
        /// 设置里的数值就是随机区间的中心值；设为 0 表示该项不随机、保持 0。
        /// </remarks>
        private static double nextRange(Random rng, double range)
        {
            if (range <= 0)
                return 0;

            const double min_factor = 0.5;
            const double max_factor = 1.5;

            return range * (min_factor + rng.NextDouble() * (max_factor - min_factor));
        }

        /// <summary>
        /// 以 <paramref name="baseValue"/> 为中心、按 ±<paramref name="amplitude"/>% 掷出一个随机值。
        /// </summary>
        private static double nextWindow(Random rng, double baseValue, double amplitude)
        {
            if (amplitude <= 0)
                return baseValue;

            double factor = 1 + (rng.NextDouble() * 2 - 1) * amplitude / 100;
            return baseValue * factor;
        }

        private static LocalisableString getRangeDescription(double value) => value.ToLocalisableString("0.#");

        public override void ResetSettingsToDefaults()
        {
            base.ResetSettingsToDefaults();

            ActiveRange = null;
            ActiveTailLenience = DEFAULT_TAIL_LENIENCE;

            ManiaHitWindows.ClearModOverride();
            TailNote.RELEASE_WINDOW_LENIENCE = DEFAULT_TAIL_LENIENCE;
        }
    }

    public static class RandomJudgementStrings
    {
        public static readonly LocalisableString DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "按种子随机每个判定的判定区间，以及 LN 面尾的判定倍数",
            "Randomise every judgement window and the LN tail multiplier with a seed");

        public static readonly LocalisableString RANGE_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "该项判定的随机区间中心值（ms），实际判定区间在其 ±50% 范围内随机；0 表示该项不随机",
            "Centre of the random range for this window (ms); the actual window rolls within ±50% of it, 0 keeps it unrandomised");

        public static readonly LocalisableString PERFECT_LABEL = new EzLocalizationManager.EzLocalisableString("Perfect", "Perfect");

        public static readonly LocalisableString GREAT_LABEL = new EzLocalizationManager.EzLocalisableString("Great", "Great");

        public static readonly LocalisableString GOOD_LABEL = new EzLocalizationManager.EzLocalisableString("Good", "Good");

        public static readonly LocalisableString OK_LABEL = new EzLocalizationManager.EzLocalisableString("Ok", "Ok");

        public static readonly LocalisableString MEH_LABEL = new EzLocalizationManager.EzLocalisableString("Meh", "Meh");

        public static readonly LocalisableString MISS_LABEL = new EzLocalizationManager.EzLocalisableString("Miss", "Miss");

        public static readonly LocalisableString TAIL_LENIENCE_LABEL = new EzLocalizationManager.EzLocalisableString("面尾判定倍数", "Tail multiplier");

        public static readonly LocalisableString TAIL_LENIENCE_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "LN 面尾判定倍数（默认 1.5x）的随机幅度（%）",
            "Randomisation amplitude (%) of the LN tail multiplier (1.5x by default)");

        public static readonly LocalisableString RANDOM = new EzLocalizationManager.EzLocalisableString("随机", "Random");

        public static readonly LocalisableString ROLLED_LABEL = new EzLocalizationManager.EzLocalisableString("本次判定区间", "Rolled windows");

        public static readonly LocalisableString TAIL_LENIENCE_ROLLED_LABEL = new EzLocalizationManager.EzLocalisableString("本次面尾倍数", "Rolled tail multiplier");
    }
}
