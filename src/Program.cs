using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace Blockganizer
{
    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            bool first;
            using (var single = new Mutex(true, @"Local\Blockganizer.Desktop", out first))
            {
                if (!first)
                {
                    IntPtr window = FindWindow(null, "Blockganizer");
                    if (window != IntPtr.Zero)
                    {
                        if (IsIconic(window)) ShowWindow(window, 9);
                        SetForegroundWindow(window);
                    }
                    return 0;
                }
                try
                {
                    string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Blockganizer");
                    using (var store = new Store(Path.Combine(folder, "data", "blockganizer.sqlite3")))
                    {
                        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
                        return app.Run(new MainWindow(store));
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show("No se pudo iniciar Blockganizer.\n\n" + error.Message, "Blockganizer",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return 1;
                }
            }
        }

        [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern IntPtr FindWindow(string className, string name);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    }
}