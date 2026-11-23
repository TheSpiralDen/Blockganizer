using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Blockganizer
{
    internal sealed class Sqlite : IDisposable
    {
        private const int Row = 100, Done = 101;
        private static readonly IntPtr Transient = new IntPtr(-1);
        private IntPtr connection;

        public Sqlite(string path)
        {
            try
            {
                Check(sqlite3_open_v2(Utf8(path), out connection, 2 | 4 | 0x10000, IntPtr.Zero));
                Check(sqlite3_busy_timeout(connection, 4000));
                Execute("PRAGMA foreign_keys = ON");
            }
            catch { Dispose(); throw; }
        }

        public long LastId { get { return sqlite3_last_insert_rowid(connection); } }
        public void Execute(string sql, params object[] args)
        {
            Run(sql, null, args);
        }
        public List<T> Query<T>(string sql, Func<Reader,T> read, params object[] args)
        {
            var result = new List<T>();
            Run(sql, row => result.Add(read(row)), args);
            return result;
        }
        public void Transaction(Action action)
        {
            Execute("BEGIN IMMEDIATE");
            try { action(); Execute("COMMIT"); }
            catch { Execute("ROLLBACK"); throw; }
        }
        private void Run(string sql, Action<Reader> read, object[] args)
        {
            IntPtr statement;
            Check(sqlite3_prepare_v2(connection, Utf8(sql), -1, out statement, IntPtr.Zero));
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    object value = args[i];
                    var str = value as string;
                    int index = i + 1;
                    if (value == null) Check(sqlite3_bind_null(statement, index));
                    else if (str != null) Check(sqlite3_bind_text(statement, index, Utf8(str), Encoding.UTF8.GetByteCount(str), Transient));
                    else Check(sqlite3_bind_int64(statement, index, Convert.ToInt64(value)));
                }
                int result;
                while ((result = sqlite3_step(statement)) == Row)
                    if (read != null) read(new Reader(statement));
                if (result != Done) Check(result);
            }
            finally { sqlite3_finalize(statement); }
        }
        public void Dispose()
        {
            if (connection == IntPtr.Zero) return;
            sqlite3_close_v2(connection);
            connection = IntPtr.Zero;
        }
        private void Check(int result)
        {
            if (result != 0) throw new InvalidOperationException("SQLite (" + result + "): " + ReadUtf8(sqlite3_errmsg(connection)));
        }
        private static byte[] Utf8(string value) { return Encoding.UTF8.GetBytes(value + "\0"); }
        private static string ReadUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return "";
            int bytes = 0;
            while (Marshal.ReadByte(ptr, bytes) != 0) bytes++;
            var data = new byte[bytes];
            Marshal.Copy(ptr, data, 0, bytes);
            return Encoding.UTF8.GetString(data);
        }
        internal sealed class Reader
        {
            private readonly IntPtr statement;
            internal Reader(IntPtr value) { statement = value; }
            public long Number(int column) { return sqlite3_column_int64(statement, column); }
            public int Integer(int column) { return checked((int)Number(column)); }
            public string Text(int column) { return ReadUtf8(sqlite3_column_text(statement, column)); }
        }

        private const string Dll = "winsqlite3.dll";
        [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] private static extern int sqlite3_open_v2(byte[] path, out IntPtr db, int flags, IntPtr vfs);
        [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] private static extern int sqlite3_close_v2(IntPtr db);
        [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] private static extern int sqlite3_busy_timeout(IntPtr db, int milliseconds);
        [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] private static extern IntPtr sqlite3_errmsg(IntPtr db);
        [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] private static extern int sqlite3_prepare_v2(IntPtr db, byte[] sql, int length, out IntPtr stmt, IntPtr tail);
        [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] private static extern int sqlite3_bind_null(IntPtr stmt, int index);
        [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] private static extern int sqlite3_bind_int64(IntPtr stmt, int index, long value);
        [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] private static extern int sqlite3_bind_text(IntPtr stmt, int index, byte[] value, int length, IntPtr destroy);
        [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] private static extern int sqlite3_step(IntPtr stmt);
        [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] private static extern int sqlite3_finalize(IntPtr stmt);
        [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] private static extern long sqlite3_last_insert_rowid(IntPtr db);
        [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] private static extern long sqlite3_column_int64(IntPtr stmt, int column);
        [DllImport(Dll, CallingConvention=CallingConvention.Cdecl)] private static extern IntPtr sqlite3_column_text(IntPtr stmt, int column);
    }
}