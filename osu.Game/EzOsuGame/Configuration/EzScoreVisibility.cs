// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game.Graphics.Sprites;

namespace osu.Game.EzOsuGame.Configuration
{
    /// <summary>
    /// 「隐藏成绩」功能工具：把数字字符替换为 '*'，其它符号（% x / , 等）保持不变。
    /// </summary>
    public static class EzScoreVisibility
    {
        /// <summary>
        /// 将 <paramref name="text"/> 中所有数字字符（0-9）替换为 '*'，其余字符保持不变。
        /// </summary>
        public static string Mask(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return text ?? string.Empty;

            char[] chars = new char[text.Length];

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                chars[i] = c >= '0' && c <= '9' ? '*' : c;
            }

            return new string(chars);
        }

        /// <summary>
        /// 创建一个跟随 <paramref name="source"/> 变化的掩码字符串绑定：
        /// 当 <paramref name="hideScores"/> 为 true 时数字显示为 '*'，否则原样显示。
        /// 返回值应被持有（例如赋给 sprite 的 <c>Current</c> 绑定）。
        /// </summary>
        public static Bindable<string> CreateMaskedBindable(IBindable<string> source, IBindable<bool> hideScores)
        {
            var masked = new Bindable<string>(hideScores.Value ? Mask(source.Value) : source.Value);

            source.BindValueChanged(v => masked.Value = hideScores.Value ? Mask(v.NewValue) : v.NewValue);
            hideScores.BindValueChanged(h => masked.Value = h.NewValue ? Mask(source.Value) : source.Value);

            return masked;
        }
    }

    /// <summary>
    /// 开启「隐藏成绩」时自动把 <see cref="Text"/> 中的数字替换为 '*' 的 <see cref="OsuSpriteText"/>。
    /// 注意：仅拦截通过本类引用设置的 <see cref="Text"/>，不拦截 sprite 的 <c>Current</c> 绑定驱动的更新。
    /// </summary>
    public partial class EzMaskedSpriteText : OsuSpriteText
    {
        [Resolved]
        private Ez2ConfigManager ezConfig { get; set; } = null!;

        private IBindable<bool> hideScores = null!;

        private LocalisableString rawText = string.Empty;

        [BackgroundDependencyLoader]
        private void load()
        {
            hideScores = ezConfig.GetBindable<bool>(Ez2Setting.HideScores);
            hideScores.BindValueChanged(_ => updateMaskedText(), true);
        }

        public new LocalisableString Text
        {
            get => base.Text;
            set
            {
                rawText = value;
                updateMaskedText();
            }
        }

        private void updateMaskedText()
        {
            if (hideScores == null)
            {
                base.Text = rawText;
                return;
            }

            base.Text = hideScores.Value ? EzScoreVisibility.Mask(rawText.ToString()) : rawText;
        }
    }
}
