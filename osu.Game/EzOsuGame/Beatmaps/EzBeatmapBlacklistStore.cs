// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Data.Sqlite;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Game.EzOsuGame.Beatmaps
{
    /// <summary>
    /// 谱面浏览器（在线谱面列表）的黑名单存储。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 使用独立的 SQLite 文件 <see cref="DATABASE_FILENAME"/>（与 ez-analysis / ez-local-profile 等既有数据库互不相干），
    /// 每行只保存 <c>online_id</c> 与展示用的 <c>title</c>。
    /// </para>
    /// <para>
    /// 启动时一次性把记录读进内存，筛选全部在内存里做 O(1) 查找；每次增删改只写一行，不重写整张表或整个文件。
    /// 因此黑名单条目数量基本不影响筛图速度（每页只是约 50 次内存哈希查找）。
    /// </para>
    /// </remarks>
    public class EzBeatmapBlacklistStore
    {
        /// <summary>
        /// 黑名单数据库文件名（位于 lazer 数据目录根部，与 client.realm 同级）。
        /// </summary>
        public const string DATABASE_FILENAME = "ez-beatmap-blacklist.sqlite";

        private const string table_name = "beatmap_blacklist";

        private readonly Storage storage;
        private readonly Lock listLock = new Lock();

        /// <summary>
        /// 谱面集 id → 标题（按 id 倒序，最近上榜的谱面排在前面，见 <see cref="Entries"/>）。
        /// </summary>
        private readonly Dictionary<int, string> entries = new Dictionary<int, string>();

        public EzBeatmapBlacklistStore(Storage storage)
        {
            this.storage = storage;
            Reload();
        }

        /// <summary>
        /// 当前黑名单条目数量。
        /// </summary>
        public int Count
        {
            get
            {
                using (listLock.EnterScope())
                    return entries.Count;
            }
        }

        /// <summary>
        /// 黑名单中的所有条目；谱面集 id 从大到小（新谱面在前），供黑名单界面展示。
        /// </summary>
        public IReadOnlyList<EzBeatmapBlacklistEntry> Entries
        {
            get
            {
                using (listLock.EnterScope())
                    return entries.OrderByDescending(pair => pair.Key).Select(pair => new EzBeatmapBlacklistEntry(pair.Key, pair.Value)).ToArray();
            }
        }

        /// <summary>
        /// 该谱面集是否处于黑名单中。
        /// </summary>
        public bool Contains(int onlineId)
        {
            using (listLock.EnterScope())
                return entries.ContainsKey(onlineId);
        }

        /// <summary>
        /// 把一张谱面加入黑名单（已存在则更新标题）。
        /// </summary>
        public void Add(EzBeatmapBlacklistEntry entry)
        {
            if (entry.OnlineId <= 0)
                return;

            using (listLock.EnterScope())
            {
                if (entries.TryGetValue(entry.OnlineId, out string? existingTitle) && existingTitle == entry.Title)
                    return;

                entries[entry.OnlineId] = entry.Title;
            }

            execute($"INSERT INTO {table_name} (online_id, title) VALUES ($online_id, $title) ON CONFLICT(online_id) DO UPDATE SET title = excluded.title;",
                entry.OnlineId, entry.Title);
        }

        /// <summary>
        /// 补写某条记录的标题（用于旧版本只存了 id、以及谱面标题发生变化的情况）。
        /// </summary>
        public void UpdateTitle(int onlineId, string title)
        {
            if (string.IsNullOrEmpty(title))
                return;

            using (listLock.EnterScope())
            {
                if (!entries.TryGetValue(onlineId, out string? existingTitle) || existingTitle == title)
                    return;

                entries[onlineId] = title;
            }

            execute($"UPDATE {table_name} SET title = $title WHERE online_id = $online_id;", onlineId, title);
        }

        /// <summary>
        /// 把一张谱面移出黑名单。
        /// </summary>
        public void Remove(int onlineId)
        {
            bool changed;

            using (listLock.EnterScope())
                changed = entries.Remove(onlineId);

            if (changed)
                execute($"DELETE FROM {table_name} WHERE online_id = $online_id;", onlineId);
        }

        /// <summary>
        /// 切换一张谱面的黑名单状态，返回切换后是否处于黑名单中。
        /// </summary>
        public bool Toggle(EzBeatmapBlacklistEntry entry)
        {
            if (Contains(entry.OnlineId))
            {
                Remove(entry.OnlineId);
                return false;
            }

            Add(entry);
            return true;
        }

        /// <summary>
        /// 从数据库重新读取黑名单（数据库尚未创建时视为空列表）。读写失败只记日志，不影响游戏运行。
        /// </summary>
        public void Reload()
        {
            try
            {
                // 没有实际使用过黑名单时不创建数据库文件。
                if (!storage.Exists(DATABASE_FILENAME))
                {
                    using (listLock.EnterScope())
                        entries.Clear();

                    return;
                }

                var loaded = new List<EzBeatmapBlacklistEntry>();

                using (var connection = openConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT online_id, title FROM {table_name} ORDER BY online_id DESC;";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            loaded.Add(new EzBeatmapBlacklistEntry(reader.GetInt32(0), reader.IsDBNull(1) ? string.Empty : reader.GetString(1)));
                    }
                }

                using (listLock.EnterScope())
                {
                    entries.Clear();

                    foreach (var entry in loaded)
                        entries[entry.OnlineId] = entry.Title;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load Ez beatmap blacklist");
            }
        }

        private void execute(string sql, int onlineId, string? title = null)
        {
            try
            {
                using (var connection = openConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.Parameters.AddWithValue("$online_id", onlineId);

                    if (title != null)
                        command.Parameters.AddWithValue("$title", title);

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save Ez beatmap blacklist");
            }
        }

        private SqliteConnection openConnection()
        {
            var connection = new SqliteConnection($"Data Source={storage.GetFullPath(DATABASE_FILENAME, true)};Cache=Shared;Mode=ReadWriteCreate");
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                // 黑名单只是本地偏好，极端情况下丢掉最后一次点击无关紧要；
                // WAL + NORMAL 让每次增删不必等待 fsync，避免点击按钮时卡住 update 线程。
                command.CommandText = $@"
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;
CREATE TABLE IF NOT EXISTS {table_name} (
    online_id INTEGER PRIMARY KEY,
    title TEXT NOT NULL DEFAULT ''
);
";
                command.ExecuteNonQuery();
            }

            ensureTitleColumn(connection);

            return connection;
        }

        /// <summary>
        /// 早期版本的表只有 <c>online_id</c> 一列，这里原地补上 <c>title</c>（已有数据保留，标题留空待补写）。
        /// </summary>
        private static void ensureTitleColumn(SqliteConnection connection)
        {
            using (var check = connection.CreateCommand())
            {
                check.CommandText = $"PRAGMA table_info({table_name});";

                using (var reader = check.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (string.Equals(reader.GetString(1), "title", StringComparison.OrdinalIgnoreCase))
                            return;
                    }
                }
            }

            using (var alter = connection.CreateCommand())
            {
                alter.CommandText = $"ALTER TABLE {table_name} ADD COLUMN title TEXT NOT NULL DEFAULT '';";
                alter.ExecuteNonQuery();
            }
        }
    }
}
