using System.Collections.Generic;
using System.Windows.Media;

namespace Blockganizer
{
    internal static class InstrumentGlyphs
    {
        private static readonly Dictionary<string, Geometry> Symbols = new Dictionary<string, Geometry>
        {
            // Preserve the icon values saved by earlier versions.
            { "♫", Shape("F0 M 2,4 L 22,4 Q 24,4 24,6 L 24,18 Q 24,20 22,20 L 2,20 Q 0,20 0,18 L 0,6 Q 0,4 2,4 Z " +
                "M 2,6 H 5 V 13 H 6 V 18 H 2 Z M 7,13 H 9 V 6 H 11 V 13 H 12 V 18 H 7 Z " +
                "M 13,13 H 15 V 6 H 17 V 13 H 18 V 18 H 13 Z M 19,13 H 21 V 6 H 22 V 18 H 19 Z") },
            { "♪", Shape("F0 M 18,1 L 21,0 L 24,3 L 23,6 L 21,6 L 15,12 C 16,16 13,18 11,18 " +
                "C 9,22 6,24 3,22 C 0,20 0,17 2,14 C 3,12 5,12 7,12 C 7,10 9,8 12,9 L 18,3 Z " +
                "M 10,12 A 2.1,2.1 0 1 1 10,16.2 A 2.1,2.1 0 1 1 10,12 Z") },
            { "♬", Shape("F0 M 21,0 L 24,1 L 23,5 L 21,5 L 11,15 L 13,17 C 13,20 10,23 6,24 " +
                "C 3,24 1,22 1,20 C 1,17 3,15 5,15 L 5,11 C 5,10 6,9 7,10 L 9,12 L 19,2 Z " +
                "M 4,19 L 7,16 L 8,17 L 5,20 Z M 6,21 L 9,18 L 10,19 L 7,22 Z") }
        };

        public static Geometry For(string symbol)
        {
            Geometry shape;
            return Symbols.TryGetValue(symbol, out shape) ? shape : null;
        }

        private static Geometry Shape(string data)
        {
            var shape = Geometry.Parse(data);
            shape.Freeze();
            return shape;
        }
    }
}
