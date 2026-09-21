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
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.Mods;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.CommunityMod
{
    /// <summary>
    /// 开谱时按种子在指定区间内随机一个 OverallDifficulty。
    /// </summary>
    /// <remarks>
    /// 为了降低代码量，本 Mod 当且仅当 HitMode 为 Lazer 时生效。
    /// <para>
    /// OD 在 <c>IApplicableToDifficulty</c> 阶段写入，早于音符 <c>ApplyDefaults</c> 时用 OD 烘焙判定区间，
    /// 因此所有物件的判定区间都会跟着随机 OD 一起变化；随机值只取决于种子，游玩 / 回放 / 成绩重算一致。
    /// </para>
    /// </remarks>
    public class ManiaModRandomOverallDifficulty : Mod, IApplicableToDifficulty, IHasSeed
    {
        public override string Name => "Random OD";

        public override string Acronym => "ROD";

        public override LocalisableString Description => RandomOverallDifficultyStrings.DESCRIPTION;

        public override IconUsage? Icon => FontAwesome.Solid.Random;

        public override ModType Type => ModType.CommunityMod;

        public override bool Ranked => false;

        public override bool ValidForMultiplayer => true;

        public override bool ValidForFreestyleAsRequiredMod => false;

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
        [SettingSource(typeof(EzCommonModStrings), nameof(EzCommonModStrings.SEED_LABEL), nameof(EzCommonModStrings.SEED_DESCRIPTION))]
        public Bindable<int?> Seed { get; } = new Bindable<int?>();

        /// <summary>
        /// 相对谱面 OD 的随机浮动上限，例如 3 表示在 ±3 OD 内随机。
        /// </summary>
        [SettingSource(typeof(RandomOverallDifficultyStrings), nameof(RandomOverallDifficultyStrings.RANGE_LABEL), nameof(RandomOverallDifficultyStrings.RANGE_DESCRIPTION))]
        public BindableDouble Range { get; } = new BindableDouble(3)
        {
            MinValue = 0,
            MaxValue = 10,
            Precision = 0.5
        };

        /// <summary>
        /// 最近一次掷出的 OD；未启用（或非 Lazer HitMode）时为 <c>null</c>。
        /// </summary>
        public double? ActiveOverallDifficulty { get; private set; }

        public override IEnumerable<(LocalisableString setting, LocalisableString value)> SettingDescription
        {
            get
            {
                yield return (EzCommonModStrings.SEED_LABEL, Seed.Value?.ToString() ?? RandomOverallDifficultyStrings.RANDOM);
                yield return (RandomOverallDifficultyStrings.RANGE_LABEL, $"{Range.Value:N1}");

                if (ActiveOverallDifficulty is double od)
                    yield return (RandomOverallDifficultyStrings.ROLLED_LABEL, $"{od:N1}");
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
                ActiveOverallDifficulty = null;
                return;
            }

            double randomised = RollOverallDifficulty(difficulty.OverallDifficulty);

            ActiveOverallDifficulty = randomised;
            difficulty.OverallDifficulty = (float)randomised;
        }

        /// <summary>
        /// 按种子在 [OD - Range, OD + Range] 内掷出一个 OD，并限制在合法的 0~10 区间。
        /// </summary>
        public double RollOverallDifficulty(double baseOverallDifficulty)
        {
            var rng = new Random(EzModSeed.Resolve(Seed));

            double offset = (rng.NextDouble() * 2 - 1) * Range.Value;

            return Math.Clamp(baseOverallDifficulty + offset, 0, 10);
        }

        public override void ResetSettingsToDefaults()
        {
            base.ResetSettingsToDefaults();

            ActiveOverallDifficulty = null;
        }
    }

    public static class RandomOverallDifficultyStrings
    {
        public static readonly LocalisableString DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "开谱时按种子随机 OverallDifficulty（仅 Lazer 判定模式生效）",
            "Randomise the OverallDifficulty of a beatmap on load (Lazer hit mode only)");

        public static readonly LocalisableString RANGE_LABEL = new EzLocalizationManager.EzLocalisableString("随机范围", "Random range");

        public static readonly LocalisableString RANGE_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "在谱面 OD 的 ±该范围内随机，结果为 0~10 之间的 OD",
            "Randomise within ±this range of the beatmap OD, resulting in an OD between 0 and 10");

        public static readonly LocalisableString ROLLED_LABEL = new EzLocalizationManager.EzLocalisableString("本次 OD", "Rolled OD");

        public static readonly LocalisableString RANDOM = new EzLocalizationManager.EzLocalisableString("随机", "Random");
    }
}
