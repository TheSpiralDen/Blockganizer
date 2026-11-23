using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Blockganizer
{
    internal sealed class Store : IDisposable
    {
        private readonly Sqlite db;
        public Store(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            db = new Sqlite(path);
            try
            {
                db.Execute("PRAGMA journal_mode = WAL");
                db.Transaction(() =>
                {
                    db.Execute(@"CREATE TABLE IF NOT EXISTS activities (
                        id INTEGER PRIMARY KEY, name TEXT NOT NULL, category TEXT NOT NULL, icon TEXT NOT NULL,
                        color TEXT NOT NULL, description TEXT NOT NULL, minutes INTEGER NOT NULL CHECK(minutes > 0),
                        status INTEGER NOT NULL DEFAULT 0 CHECK(status BETWEEN 0 AND 2))");
                    db.Execute(@"CREATE TABLE IF NOT EXISTS blocks (
                        id INTEGER PRIMARY KEY, activity_id INTEGER NOT NULL REFERENCES activities(id),
                        day TEXT NOT NULL, position INTEGER NOT NULL, done INTEGER NOT NULL DEFAULT 0 CHECK(done IN (0, 1)),
                        name TEXT NOT NULL, category TEXT NOT NULL, icon TEXT NOT NULL, color TEXT NOT NULL,
                        description TEXT NOT NULL, minutes INTEGER NOT NULL CHECK(minutes > 0))");
                    AddBlockFields();
                    db.Execute("CREATE INDEX IF NOT EXISTS blocks_day ON blocks(day, position)");
                    db.Execute(@"CREATE TABLE IF NOT EXISTS goals (
                        activity_id INTEGER NOT NULL REFERENCES activities(id), since TEXT NOT NULL,
                        minutes INTEGER NOT NULL CHECK(minutes >= 0), PRIMARY KEY(activity_id, since))");
                    db.Execute("CREATE TABLE IF NOT EXISTS settings (key TEXT PRIMARY KEY, value TEXT NOT NULL)");
                    if (Setting("initialized", "") != "yes")
                    {
                        foreach (Activity starter in Catalog.Starter()) WriteActivity(starter);
                        SetSetting("initialized", "yes");
                    }
                });
            }
            catch { db.Dispose(); throw; }
        }

        private void AddBlockFields()
        {
            var existing = db.Query("PRAGMA table_info(blocks)", r => r.Text(1));
            if (!existing.Contains("note")) db.Execute("ALTER TABLE blocks ADD COLUMN note TEXT NOT NULL DEFAULT ''");
            if (!existing.Contains("priority")) db.Execute("ALTER TABLE blocks ADD COLUMN priority INTEGER NOT NULL DEFAULT 0");
            if (!existing.Contains("remaining_ms")) db.Execute("ALTER TABLE blocks ADD COLUMN remaining_ms INTEGER NOT NULL DEFAULT -1");
            if (!existing.Contains("ends_at_ms")) db.Execute("ALTER TABLE blocks ADD COLUMN ends_at_ms INTEGER NOT NULL DEFAULT 0");
        }

        public List<Activity> Activities(DateTime week)
        {
            return db.Query(@"SELECT a.id, a.name, a.category, a.icon, a.color, a.description, a.minutes,
                COALESCE((SELECT g.minutes FROM goals g WHERE g.activity_id = a.id AND g.since <= ?
                ORDER BY g.since DESC LIMIT 1), 0), a.status
                FROM activities a ORDER BY a.id", r => new Activity {
                    Id = r.Number(0), Name = r.Text(1), Category = r.Text(2), Icon = r.Text(3),
                    Color = r.Text(4), Description = r.Text(5), Minutes = r.Integer(6),
                    Goal = r.Integer(7), Status = r.Integer(8)
                }, Calendar.Key(week));
        }

        public List<Block> Week(DateTime monday)
        {
            return db.Query(@"SELECT id, activity_id, day, done, name, category, icon, color, description, minutes,
                    note, priority, remaining_ms, ends_at_ms
                FROM blocks WHERE day >= ? AND day < ? ORDER BY day, priority DESC, position, id", r => new Block {
                    Id = r.Number(0), ActivityId = r.Number(1), Date = Calendar.Parse(r.Text(2)),
                    Done = r.Integer(3) == 1, Name = r.Text(4), Category = r.Text(5), Icon = r.Text(6),
                    Color = r.Text(7), Description = r.Text(8), Minutes = r.Integer(9), Note = r.Text(10),
                    Priority = r.Integer(11) != 0, RemainingMilliseconds = r.Number(12), EndsAtMilliseconds = r.Number(13)
                }, Calendar.Key(monday), Calendar.Key(monday.AddDays(7)));
        }

        public long Save(Activity activity, DateTime week)
        {
            long id = 0;
            db.Transaction(() =>
            {
                id = WriteActivity(activity);
                int oldGoal = db.Query(@"SELECT minutes FROM goals WHERE activity_id = ? AND since <= ?
                    ORDER BY since DESC LIMIT 1", r => r.Integer(0), id, Calendar.Key(week)).FirstOrDefault();
                if (oldGoal != activity.Goal) WriteGoal(id, week, activity.Goal);
            });
            return id;
        }
        private long WriteActivity(Activity a)
        {
            if (a.Id == 0)
            {
                db.Execute(@"INSERT INTO activities(name, category, icon, color, description, minutes)
                    VALUES(?, ?, ?, ?, ?, ?)", a.Name, a.Category, a.Icon, a.Color, a.Description, a.Minutes);
                return db.LastId;
            }
            db.Execute(@"UPDATE activities SET name=?, category=?, icon=?, color=?, description=?, minutes=?
                WHERE id=? AND status <> 2", a.Name, a.Category, a.Icon, a.Color, a.Description, a.Minutes, a.Id);
            return a.Id;
        }
        public void SetStatus(long id, int status)
        {
            db.Execute("UPDATE activities SET status=? WHERE id=? AND status <> 2", status, id);
        }
        private void WriteGoal(long id, DateTime week, int minutes)
        {
            db.Execute("INSERT OR REPLACE INTO goals(activity_id, since, minutes) VALUES(?, ?, ?)",
                id, Calendar.Key(week), minutes);
        }
        public void Plan(long activityId, DateTime date, long? anchor = null, bool after = false)
        {
            string day = Calendar.Key(date);
            db.Transaction(() =>
            {
                db.Execute(@"INSERT INTO blocks(activity_id, day, position, name, category, icon, color, description, minutes)
                    SELECT id, ?, COALESCE((SELECT MAX(position) + 1 FROM blocks WHERE day = ?), 0),
                    name, category, icon, color, description, minutes FROM activities WHERE id = ? AND status = 0",
                    day, day, activityId);
                if (db.Query("SELECT changes()", r => r.Integer(0))[0] == 0)
                    throw new InvalidOperationException("La actividad ya no está disponible.");
                if (anchor.HasValue) Reorder(db.LastId, day, anchor, after);
            });
        }
        public void Move(long id, DateTime date, long? anchor = null, bool after = false)
        {
            if (id == anchor) return;
            db.Transaction(() => Reorder(id, Calendar.Key(date), anchor, after));
        }
        private void Reorder(long id, string day, long? anchor, bool after)
        {
            int priority = db.Query("SELECT priority FROM blocks WHERE id=?", r => r.Integer(0), id).Single();
            var order = db.Query("SELECT id FROM blocks WHERE day = ? AND priority = ? AND id <> ? ORDER BY position, id",
                r => r.Number(0), day, priority, id);
            int index = anchor.HasValue ? order.IndexOf(anchor.Value) : -1;
            order.Insert(index < 0 ? order.Count : index + (after ? 1 : 0), id);
            for (int i = 0; i < order.Count; i++)
                db.Execute("UPDATE blocks SET day=?, position=? WHERE id=?", day, i, order[i]);
        }
        public void SetNote(long id, string note)
        {
            db.Execute("UPDATE blocks SET note=? WHERE id=?", note, id);
        }

        public void SetPriority(long id, bool priority)
        {
            db.Execute(@"UPDATE blocks SET priority=?, position=(
                    SELECT COALESCE(MIN(other.position), 0) - 1 FROM blocks other
                    WHERE other.day=blocks.day AND other.priority=? AND other.id<>blocks.id)
                WHERE id=? AND priority<>?", priority, priority, id, priority);
        }

        public void ToggleTimer(long id, long now)
        {
            db.Transaction(() =>
            {
                CompleteExpiredTimers(now);
                var target = db.Query("SELECT ends_at_ms FROM blocks WHERE id=? AND done=0", r => r.Number(0), id);
                if (target.Count == 0) return;

                db.Execute("UPDATE blocks SET remaining_ms=MIN(minutes*60000, ends_at_ms-?), ends_at_ms=0 WHERE ends_at_ms>?",
                    now, now);
                if (target[0] == 0)
                {
                    db.Execute(@"UPDATE blocks SET ends_at_ms=? +
                        CASE WHEN remaining_ms < 0 THEN minutes * 60000 ELSE remaining_ms END WHERE id=?",
                        now, id);
                }
            });
        }

        public void CompleteExpiredTimers(long now)
        {
            db.Execute(@"UPDATE blocks SET done=1, remaining_ms=0, ends_at_ms=0
                WHERE ends_at_ms>0 AND ends_at_ms<=?", now);
        }

        public long TimerDeadline()
        {
            return db.Query("SELECT COALESCE(MIN(ends_at_ms), 0) FROM blocks WHERE ends_at_ms>0",
                r => r.Number(0)).Single();
        }

        public void Complete(long id, bool done)
        {
            db.Execute("UPDATE blocks SET done=?, remaining_ms=?, ends_at_ms=0 WHERE id=?", done, done ? 0 : -1, id);
        }
        public void Remove(long id) { db.Execute("DELETE FROM blocks WHERE id=?", id); }
        public string Setting(string key, string fallback)
        {
            return db.Query("SELECT value FROM settings WHERE key=?", r => r.Text(0), key).FirstOrDefault() ?? fallback;
        }
        public void SetSetting(string key, string value) { db.Execute("INSERT OR REPLACE INTO settings VALUES(?, ?)", key, value); }
        public void Dispose() { db.Dispose(); }
    }
}
