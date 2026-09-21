// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.Mods;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Scoring;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.CommunityMod
{
    /// <summary>
    /// 把 Lazer 判定的每个判定区间（以及 LN 面尾的判定倍数）按种子随机缩放。
    /// </summary>
    /// <remarks>
    /// 为了降低代码量，本 Mod 当且仅当 HitMode 为 Lazer 时生效：其它 HitMode 的判定由各自的
    /// <c>IManiaHitModeJudgement</c> 实现直接比较区间，不读取这里的随机结果。
    /// <para>
    /// 随机的基准是「当前谱面 OD + 当前 mod 组合」下的 Lazer 判定区间；随机结果通过
    /// <see cref="ManiaHitWindows.SetModOverride"/> 全局生效，因此游玩、回放、Session 重判与
    /// 选歌界面显示的区间完全一致。
    /// </para>
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

        /// <summary>Lazer 面尾判定默认倍数。</summary>
        public const double DEFAULT_TAIL_LENIENCE = 1.5D;

        /// <summary>
        /// 随机种子；留空时每次游玩重新掷一次。
        /// </summary>
        [SettingSource(typeof(EzCommonModStrings), nameof(EzCommonModStrings.SEED_LABEL), nameof(EzCommonModStrings.SEED_DESCRIPTION))]
        public Bindable<int?> Seed { get; } = new Bindable<int?>();

        [SettingSource(typeof(RandomJudgementStrings), nameof(RandomJudgementStrings.PERFECT_LABEL), nameof(RandomJudgementStrings.AMPLITUDE_DESCRIPTION))]
        public BindableDouble PerfectAmplitude { get; } = createAmplitude();

        [SettingSource(typeof(RandomJudgementStrings), nameof(RandomJudgementStrings.GREAT_LABEL), nameof(RandomJudgementStrings.AMPLITUDE_DESCRIPTION))]
        public BindableDouble GreatAmplitude { get; } = createAmplitude();

        [SettingSource(typeof(RandomJudgementStrings), nameof(RandomJudgementStrings.GOOD_LABEL), nameof(RandomJudgementStrings.AMPLITUDE_DESCRIPTION))]
        public BindableDouble GoodAmplitude { get; } = createAmplitude();

        [SettingSource(typeof(RandomJudgementStrings), nameof(RandomJudgementStrings.OK_LABEL), nameof(RandomJudgementStrings.AMPLITUDE_DESCRIPTION))]
        public BindableDouble OkAmplitude { get; } = createAmplitude();

        [SettingSource(typeof(RandomJudgementStrings), nameof(RandomJudgementStrings.MEH_LABEL), nameof(RandomJudgementStrings.AMPLITUDE_DESCRIPTION))]
        public BindableDouble MehAmplitude { get; } = createAmplitude();

        [SettingSource(typeof(RandomJudgementStrings), nameof(RandomJudgementStrings.MISS_LABEL), nameof(RandomJudgementStrings.AMPLITUDE_DESCRIPTION))]
        public BindableDouble MissAmplitude { get; } = createAmplitude();

        /// <summary>
        /// LN 面尾判定倍数的随机幅度（相对默认面尾倍数的 ±百分比），0 表示不随机。
        /// </summary>
        [SettingSource(typeof(RandomJudgementStrings), nameof(RandomJudgementStrings.TAIL_LENIENCE_LABEL), nameof(RandomJudgementStrings.TAIL_LENIENCE_DESCRIPTION))]
        public BindableDouble TailLenienceAmplitude { get; } = createAmplitude();

        /// <summary>
        /// 当前生效的随机判定区间；未启用（或非 Lazer HitMode）时为 <c>null</c>。
        /// </summary>
        public ManiaModifyHitRange? ActiveRange { get; private set; }

        /// <summary>
        /// 当前生效的 LN 面尾判定倍数。
        /// </summary>
        public double ActiveTailLenience { get; private set; } = DEFAULT_TAIL_LENIENCE;

        private static BindableDouble createAmplitude() => new BindableDouble
        {
            MinValue = 0,
            MaxValue = 100,
            Precision = 1
        };

        public override IEnumerable<(LocalisableString setting, LocalisableString value)> SettingDescription
        {
            get
            {
                yield return (EzCommonModStrings.SEED_LABEL, Seed.Value?.ToString() ?? RandomJudgementStrings.RANDOM);
                yield return (RandomJudgementStrings.PERFECT_LABEL, $"{PerfectAmplitude.Value:N0} %");
                yield return (RandomJudgementStrings.GREAT_LABEL, $"{GreatAmplitude.Value:N0} %");
                yield return (RandomJudgementStrings.GOOD_LABEL, $"{GoodAmplitude.Value:N0} %");
                yield return (RandomJudgementStrings.OK_LABEL, $"{OkAmplitude.Value:N0} %");
                yield return (RandomJudgementStrings.MEH_LABEL, $"{MehAmplitude.Value:N0} %");
                yield return (RandomJudgementStrings.MISS_LABEL, $"{MissAmplitude.Value:N0} %");
                yield return (RandomJudgementStrings.TAIL_LENIENCE_LABEL, $"{TailLenienceAmplitude.Value:N0} %");

                if (ActiveRange is { } range)
                {
                    yield return (RandomJudgementStrings.ROLLED_LABEL,
                        $"P {range.Perfect:0.#} / G {range.Great:0.#} / Gd {range.Good:0.#} / Ok {range.Ok:0.#} / M {range.Meh:0.#} / Miss {range.Miss:0.#} ms");

                    yield return (RandomJudgementStrings.TAIL_LENIENCE_ROLLED_LABEL, $"{ActiveTailLenience:N2}x");
                }
            }
        }

        /// <summary>
        /// 本 Mod 只在 Lazer HitMode 下生效。
        /// </summary>
        private static bool isLazerHitMode()
            => GlobalConfigStore.EzConfig.Get<EzEnumHitMode>(Ez2Setting.ManiaHitMode) == EzEnumHitMode.Lazer;

        public void ApplyToDifficulty(BeatmapDifficulty difficulty)
        {
            if (!isLazerHitMode())
            {
                ActiveRange = null;
                ActiveTailLenience = DEFAULT_TAIL_LENIENCE;
                return;
            }

            ManiaModifyHitRange? range = ComputeRange(difficulty.OverallDifficulty, out double tailLenience);

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
        /// <remarks>
        /// 这里只拿得到 <see cref="IBeatmapInfo"/>，按谱面自身 OD 计算。若同时开启 HR / EZ（它们直接改判定区间而不是 OD），
        /// 显示值会与游玩值存在偏差；游玩本身始终使用经过全部 Mod 调整后的 OD。
        /// </remarks>
        public ManiaModifyHitRange? GetDisplayHitRange(IBeatmapInfo beatmapInfo)
        {
            if (!isLazerHitMode())
                return null;

            return ComputeRange(beatmapInfo.Difficulty.OverallDifficulty, out _);
        }

        /// <summary>
        /// 按种子掷出六个判定区间的随机值以及 LN 面尾倍数。
        /// </summary>
        /// <param name="overallDifficulty">参与计算的 OD（随机结果以该 OD 的 Lazer 区间为中心）。</param>
        /// <param name="tailLenience">掷出的 LN 面尾判定倍数。</param>
        /// <returns>需要覆写判定区间时返回随机结果；六个区间都没有设置随机时返回 <c>null</c>。</returns>
        private ManiaModifyHitRange? ComputeRange(double overallDifficulty, out double tailLenience)
        {
            tailLenience = DEFAULT_TAIL_LENIENCE;

            double[] amplitudes =
            [
                PerfectAmplitude.Value,
                GreatAmplitude.Value,
                GoodAmplitude.Value,
                OkAmplitude.Value,
                MehAmplitude.Value,
                MissAmplitude.Value,
            ];

            bool anyWindowRandom = false;

            foreach (double amplitude in amplitudes)
                anyWindowRandom |= amplitude > 0;

            // 只有面尾参与随机时不需要覆写判定区间。
            bool anyTailRandom = TailLenienceAmplitude.Value > 0;

            if (!anyWindowRandom && !anyTailRandom)
                return null;

            var rng = new Random(EzModSeed.Resolve(Seed));

            // 掷值顺序固定，保证同一 seed 下各判定类型的结果稳定、可复现。
            double perfect = nextWindow(rng, IBeatmapDifficultyInfo.DifficultyRange(overallDifficulty, ManiaHitWindows.PERFECT_WINDOW_RANGE), amplitudes[0]);
            double great = nextWindow(rng, IBeatmapDifficultyInfo.DifficultyRange(overallDifficulty, GREAT_WINDOW_RANGE), amplitudes[1]);
            double good = nextWindow(rng, IBeatmapDifficultyInfo.DifficultyRange(overallDifficulty, GOOD_WINDOW_RANGE), amplitudes[2]);
            double ok = nextWindow(rng, IBeatmapDifficultyInfo.DifficultyRange(overallDifficulty, OK_WINDOW_RANGE), amplitudes[3]);
            double meh = nextWindow(rng, IBeatmapDifficultyInfo.DifficultyRange(overallDifficulty, MEH_WINDOW_RANGE), amplitudes[4]);
            double miss = nextWindow(rng, IBeatmapDifficultyInfo.DifficultyRange(overallDifficulty, MISS_WINDOW_RANGE), amplitudes[5]);

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
        /// 以 <paramref name="baseValue"/> 为中心、按 ±<paramref name="amplitude"/>% 掷出一个随机值。
        /// </summary>
        private static double nextWindow(Random rng, double baseValue, double amplitude)
        {
            if (amplitude <= 0)
                return baseValue;

            double factor = 1 + (rng.NextDouble() * 2 - 1) * amplitude / 100;
            return baseValue * factor;
        }

        public override void ResetSettingsToDefaults()
        {
            base.ResetSettingsToDefaults();

            ActiveRange = null;
            ActiveTailLenience = DEFAULT_TAIL_LENIENCE;

            ManiaHitWindows.ClearModOverride();
            TailNote.RELEASE_WINDOW_LENIENCE = DEFAULT_TAIL_LENIENCE;
        }

        // 与 ManiaHitWindows 中的私有区间保持一致（Lazer 判定使用这套区间）。
        private static readonly DifficultyRange GREAT_WINDOW_RANGE = new DifficultyRange(64, 49, 34);
        private static readonly DifficultyRange GOOD_WINDOW_RANGE = new DifficultyRange(97, 82, 67);
        private static readonly DifficultyRange OK_WINDOW_RANGE = new DifficultyRange(127, 112, 97);
        private static readonly DifficultyRange MEH_WINDOW_RANGE = new DifficultyRange(151, 136, 121);
        private static readonly DifficultyRange MISS_WINDOW_RANGE = new DifficultyRange(188, 173, 158);
    }

    public static class RandomJudgementStrings
    {
        public static readonly LocalisableString DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "按种子随机每个判定的判定区间，以及 LN 面尾的判定倍数（仅 Lazer 判定模式生效）",
            "Randomise every judgement window and the LN tail multiplier with a seed (Lazer hit mode only)");

        public static readonly LocalisableString AMPLITUDE_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "随机幅度（%）：在该判定原本的判定时长上下浮动这么多，0 表示不随机",
            "Randomisation amplitude (%): the window fluctuates by this much around its original duration, 0 keeps it unchanged");

        public static readonly LocalisableString PERFECT_LABEL = new EzLocalizationManager.EzLocalisableString("Perfect 随机幅度", "Perfect amplitude");

        public static readonly LocalisableString GREAT_LABEL = new EzLocalizationManager.EzLocalisableString("Great 随机幅度", "Great amplitude");

        public static readonly LocalisableString GOOD_LABEL = new EzLocalizationManager.EzLocalisableString("Good 随机幅度", "Good amplitude");

        public static readonly LocalisableString OK_LABEL = new EzLocalizationManager.EzLocalisableString("Ok 随机幅度", "Ok amplitude");

        public static readonly LocalisableString MEH_LABEL = new EzLocalizationManager.EzLocalisableString("Meh 随机幅度", "Meh amplitude");

        public static readonly LocalisableString MISS_LABEL = new EzLocalizationManager.EzLocalisableString("Miss 随机幅度", "Miss amplitude");

        public static readonly LocalisableString TAIL_LENIENCE_LABEL = new EzLocalizationManager.EzLocalisableString("面尾判定倍数", "Tail multiplier");

        public static readonly LocalisableString TAIL_LENIENCE_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "LN 面尾判定倍数（默认 1.5x）的随机幅度（%）",
            "Randomisation amplitude (%) of the LN tail multiplier (1.5x by default)");

        public static readonly LocalisableString RANDOM = new EzLocalizationManager.EzLocalisableString("随机", "Random");

        public static readonly LocalisableString ROLLED_LABEL = new EzLocalizationManager.EzLocalisableString("本次判定区间", "Rolled windows");

        public static readonly LocalisableString TAIL_LENIENCE_ROLLED_LABEL = new EzLocalizationManager.EzLocalisableString("本次面尾倍数", "Rolled tail multiplier");
    }
}
