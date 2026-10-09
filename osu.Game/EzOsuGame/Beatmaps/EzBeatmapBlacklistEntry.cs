// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.EzOsuGame.Beatmaps
{
    /// <summary>
    /// 谱面浏览器黑名单中的一条记录。
    /// </summary>
    /// <remarks>
    /// 只保存筛选需要的谱面集 <see cref="OnlineId"/> 和黑名单界面展示用的 <see cref="Title"/>，
    /// 不保存作者、封面等其它元数据。
    /// </remarks>
    public readonly record struct EzBeatmapBlacklistEntry(int OnlineId, string Title)
    {
        public static EzBeatmapBlacklistEntry FromBeatmapSet(APIBeatmapSet beatmapSet)
            => new EzBeatmapBlacklistEntry(beatmapSet.OnlineID, DisplayTitleOf(beatmapSet));

        /// <summary>
        /// 谱面卡片上展示的标题（优先使用 Unicode 原标题，与卡片本身一致）。
        /// </summary>
        public static string DisplayTitleOf(APIBeatmapSet beatmapSet)
            => string.IsNullOrEmpty(beatmapSet.TitleUnicode) ? beatmapSet.Title : beatmapSet.TitleUnicode;
    }
}