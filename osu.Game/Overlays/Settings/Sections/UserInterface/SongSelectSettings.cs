// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays.Mods.Input;

namespace osu.Game.Overlays.Settings.Sections.UserInterface
{
    public partial class SongSelectSettings : SettingsSubsection
    {
        protected override LocalisableString Header => UserInterfaceStrings.SongSelectHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, Ez2ConfigManager ezConfig)
        {
            Children = new Drawable[]
            {
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = UserInterfaceStrings.ShowConvertedBeatmaps,
                    Current = config.GetBindable<bool>(OsuSetting.ShowConvertedBeatmaps),
                })
                {
                    Keywords = new[] { "converts", "converted" }
                },
                new SettingsItemV2(new FormEnumDropdown<RandomSelectAlgorithm>
                {
                    Caption = UserInterfaceStrings.RandomSelectionAlgorithm,
                    Current = config.GetBindable<RandomSelectAlgorithm>(OsuSetting.RandomSelectAlgorithm),
                }),
                new SettingsItemV2(new FormEnumDropdown<ModSelectHotkeyStyle>
                {
                    Caption = UserInterfaceStrings.ModSelectHotkeyStyle,
                    Current = config.GetBindable<ModSelectHotkeyStyle>(OsuSetting.ModSelectHotkeyStyle),
                })
                {
                    ApplyClassicDefault = c => ((IHasCurrentValue<ModSelectHotkeyStyle>)c).Current.Value = ModSelectHotkeyStyle.Classic,
                },
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = UserInterfaceStrings.ModSelectTextSearchStartsActive,
                    Current = config.GetBindable<bool>(OsuSetting.ModSelectTextSearchStartsActive),
                })
                {
                    ApplyClassicDefault = c => ((IHasCurrentValue<bool>)c).Current.Value = false,
                },
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = GameplaySettingsStrings.BackgroundBlur,
                    Current = config.GetBindable<bool>(OsuSetting.SongSelectBackgroundBlur),
                }),
                // [Ez] 选歌界面背景暗度：独立于游玩中的「背景暗度」。
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EzSettingsStrings.SONG_SELECT_BACKGROUND_DIM,
                    HintText = EzSettingsStrings.SONG_SELECT_BACKGROUND_DIM_TOOLTIP,
                    Current = ezConfig.GetBindable<float>(Ez2Setting.SongSelectBackgroundDim),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true,
                })
                {
                    Keywords = new[] { "dim", "background", "brightness", "song select", "选歌", "背景", "暗度", "亮度", "变暗" }
                },
                // [Ez] 传送带卡片封面暗度。
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EzSettingsStrings.SONG_SELECT_COVER_DIM,
                    HintText = EzSettingsStrings.SONG_SELECT_COVER_DIM_TOOLTIP,
                    Current = ezConfig.GetBindable<float>(Ez2Setting.SongSelectCoverDim),
                    KeyboardStep = 0.01f,
                    DisplayAsPercentage = true,
                })
                {
                    Keywords = new[] { "dim", "cover", "card", "carousel", "brightness", "song select", "选歌", "传送带", "卡片", "封面", "暗度", "亮度" }
                },
            };
        }
    }
}
