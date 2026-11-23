using System;
using System.Globalization;
using System.Windows.Media;

namespace Blockganizer
{
    internal sealed class Activity
    {
        public long Id;
        public string Name, Category, Icon, Color, Description;
        public int Minutes, Goal, Status;
    }

    internal sealed class Block
    {
        public long Id, ActivityId;
        public DateTime Date;
        public int Minutes;
        public bool Done, Priority;
        public string Name, Category, Icon, Color, Description, Note;
        public long RemainingMilliseconds, EndsAtMilliseconds;

        public bool Running { get { return EndsAtMilliseconds > 0; } }

        public long TimeLeft(long now)
        {
            if (Done) return 0;
            if (Running) return Math.Min(Minutes * 60000L, Math.Max(0, EndsAtMilliseconds - now));
            return RemainingMilliseconds < 0 ? Minutes * 60000L : RemainingMilliseconds;
        }
    }

    internal static class Calendar
    {
        public static readonly CultureInfo Mexican = CultureInfo.GetCultureInfo("es-MX");
        public static DateTime Monday(DateTime day)
        {
            return day.Date.AddDays(-(((int)day.DayOfWeek + 6) % 7));
        }
        public static int Week(DateTime monday)
        {
            DateTime fourth = new DateTime(monday.AddDays(3).Year, 1, 4);
            return (monday - Monday(fourth)).Days / 7 + 1;
        }
        public static string Key(DateTime date) { return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
        public static DateTime Parse(string date) { return DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture); }
        public static string Capital(string s) { return Mexican.TextInfo.ToUpper(s.Substring(0, 1)) + s.Substring(1); }
        public static string Range(DateTime monday)
        {
            DateTime sunday = monday.AddDays(6);
            if (monday.Year != sunday.Year)
                return monday.ToString("d MMM yyyy", Mexican) + " – " + sunday.ToString("d MMM yyyy", Mexican);
            if (monday.Month == sunday.Month)
                return monday.Day + "–" + sunday.ToString("d 'de' MMMM", Mexican) +
                    (monday.Year == DateTime.Today.Year ? "" : " " + monday.Year);
            return monday.ToString("d MMM", Mexican) + " – " + sunday.ToString("d MMM, yyyy", Mexican);
        }
        public static string Duration(int minutes)
        {
            if (minutes < 60) return minutes + " min";
            return (minutes / 60) + " h" + (minutes % 60 == 0 ? "" : " " + (minutes % 60) + " min");
        }
    }

    internal sealed class Choice
    {
        public readonly string Name, Value;
        public Choice(string name, string value) { Name = name; Value = value; }
        public override string ToString() { return Name; }
    }

    internal static class Catalog
    {
        public static readonly Choice[] Icons = {
            new Choice("Bajo", "♬"), new Choice("Guitarra", "♪"), new Choice("Piano", "♫"),
            new Choice("Estudio", "▤"), new Choice("Producción", "◈"), new Choice("Movimiento", "↗"),
            new Choice("Comida", "◉"), new Choice("Lectura", "▥"), new Choice("Actividad", "◇")
        };
        public static readonly Choice[] Colors = {
            new Choice("Menta", "#71D9B3"), new Choice("Lavanda", "#B6A1F4"), new Choice("Azul", "#8CB9F7"),
            new Choice("Ámbar", "#F3C681"), new Choice("Coral", "#FA9C91"), new Choice("Rosa", "#EBACD5"),
            new Choice("Verde", "#C4D887"), new Choice("Gris", "#B4BFCC")
        };
        public static Activity[] Starter()
        {
            return new[] {
                Make("Practicar piano", "Música", "♫", 0, 45),
                Make("Practicar guitarra", "Música", "♪", 0, 30),
                Make("Practicar bajo", "Música", "♬", 0, 30),
                Make("Estudiar teoría musical", "Aprendizaje", "▤", 1, 30),
                Make("Tutorial de producción", "Aprendizaje", "◈", 1, 45),
                Make("Ejercicio", "Bienestar", "↗", 2, 20),
                Make("Comida", "Rutina", "◉", 3, 40),
                Make("Lectura", "Personal", "▥", 5, 30)
            };
        }
        private static Activity Make(string name, string category, string icon, int color, int minutes)
        {
            return new Activity { Name = name, Category = category, Icon = icon, Color = Colors[color].Value, Minutes = minutes, Description = "" };
        }
    }
}
