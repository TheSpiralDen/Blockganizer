using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Blockganizer
{
    internal static class FabricInk
    {
        private static readonly Dictionary<Color, Brush> Textures = new Dictionary<Color, Brush>();
        private static readonly Brush Weave = CreateWeave();
        private static readonly Brush[] Rich = Swatches("#AC294D", "#274AB5", "#865308", "#6B389D", "#14654F");
        private static readonly Brush[] Bright = Swatches("#FFA7BA", "#BDC9FF", "#FFDC87", "#DCC1FF", "#A2F2D3");
        private static readonly Brush RichField = ColorBands(Rich);
        private static readonly Brush BrightField = ColorBands(Bright);

        private static Brush[] Swatches(params string[] colors)
        {
            var result = new Brush[colors.Length];
            for (int i = 0; i < colors.Length; i++) result[i] = From(Palette.Paint(colors[i]));
            return result;
        }

        private static bool NeedsLightInk(Brush ink)
        {
            var solid = ink as SolidColorBrush;
            return solid != null && solid.Color.R * .21 + solid.Color.G * .72 + solid.Color.B * .07 > 152;
        }

        public static Brush[] Threads(Brush ink)
        {
            return NeedsLightInk(ink) ? Bright : Rich;
        }

        public static Brush Field(Brush ink)
        {
            return NeedsLightInk(ink) ? BrightField : RichField;
        }

        private static Brush ColorBands(Brush[] threads)
        {
            var colors = new DrawingGroup();
            for (int i = 0; i < threads.Length; i++)
            {
                double width = i + 1 == threads.Length ? 6 : 2;
                colors.Children.Add(new GeometryDrawing(threads[i], null,
                    new RectangleGeometry(new Rect(i * 2, 0, width, 1))));
            }
            var brush = new DrawingBrush(colors) { Stretch = Stretch.Fill };
            brush.Freeze();
            return brush;
        }

        public static Brush From(Brush ink)
        {
            var solid = ink as SolidColorBrush;
            if (solid == null) return ink;
            Color color = solid.Color;
            Brush cached;
            if (Textures.TryGetValue(color, out cached)) return cached;

            bool pale = NeedsLightInk(ink);
            Color white = Colors.White;
            Color black = Colors.Black;
            var sheen = new LinearGradientBrush { StartPoint = new Point(.15, 0), EndPoint = new Point(.38, 1) };
            sheen.GradientStops.Add(new GradientStop(Mix(color, black, pale ? .18 : .22), 0));
            sheen.GradientStops.Add(new GradientStop(Mix(color, white, pale ? .65 : .32), .29));
            sheen.GradientStops.Add(new GradientStop(Mix(color, white, pale ? .28 : .06), .48));
            sheen.GradientStops.Add(new GradientStop(color, .68));
            sheen.GradientStops.Add(new GradientStop(Mix(color, black, pale ? .22 : .34), 1));
            var tile = new RectangleGeometry(new Rect(0, 0, 96, 24));
            var textile = new DrawingGroup();
            textile.Children.Add(new GeometryDrawing(sheen, null, tile));
            textile.Children.Add(new GeometryDrawing(Weave, null, tile));
            var brush = new DrawingBrush(textile) { Stretch = Stretch.Fill };
            brush.Freeze();
            Textures.Add(color, brush);
            return brush;
        }

        private static Color Mix(Color from, Color to, double fraction)
        {
            return Color.FromArgb(from.A,
                (byte)(from.R + (to.R - from.R) * fraction),
                (byte)(from.G + (to.G - from.G) * fraction),
                (byte)(from.B + (to.B - from.B) * fraction));
        }

        private static Brush CreateWeave()
        {
            var tile = new DrawingGroup();
            tile.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 3, 3))));
            tile.Children.Add(new GeometryDrawing(null, new Pen(Palette.Paint("#31FFFFFF"), .5),
                Geometry.Parse("M 0,1 L 1,0 M 0,3 L 3,0 M 2,3 L 3,2")));
            tile.Children.Add(new GeometryDrawing(null, new Pen(Palette.Paint("#24000000"), .5),
                new LineGeometry(new Point(0, 2), new Point(2, 0))));
            var brush = new DrawingBrush(tile) {
                TileMode = TileMode.Tile, ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, 3, 3), Stretch = Stretch.Fill
            };
            brush.Freeze();
            return brush;
        }
    }

    internal sealed class FabricLabel : TextBlock
    {
        private Brush[] threads;

        static FabricLabel()
        {
            TextProperty.OverrideMetadata(typeof(FabricLabel), new FrameworkPropertyMetadata("", TextChanged));
        }

        public void UseInk(Brush ink)
        {
            Foreground = FabricInk.From(ink);
            threads = FabricInk.Threads(ink);
            UpdateThreads();
        }

        private static void TextChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var label = (FabricLabel)source;
            if (label.threads != null) label.UpdateThreads();
        }

        private void UpdateThreads()
        {
            var effects = new TextEffectCollection();
            var characters = StringInfo.GetTextElementEnumerator(Text);
            int next = 0;
            while (characters.MoveNext())
            {
                string character = characters.GetTextElement();
                if (!char.IsLetterOrDigit(character, 0)) continue;
                effects.Add(new TextEffect(null, threads[next % threads.Length], null, characters.ElementIndex, character.Length));
                next++;
            }
            effects.Freeze();
            TextEffects = effects;
        }
    }

    internal sealed class FabricHeading : FrameworkElement
    {
        private readonly TextBlock label;

        private FabricHeading(TextBlock label)
        {
            this.label = label;
            IsHitTestVisible = false;
            label.SizeChanged += (_, __) => InvalidateVisual();
        }

        public static Grid Wrap(TextBlock label)
        {
            var layers = new Grid();
            layers.Children.Add(label);
            layers.Children.Add(new FabricHeading(label));
            return layers;
        }

        protected override void OnRender(DrawingContext drawing)
        {
            var text = new FormattedText(label.Text, label.Language.GetSpecificCulture(), label.FlowDirection,
                new Typeface(label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch), label.FontSize, label.Foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            if (label.Text.Length == 0 || text.Width > ActualWidth + 1 || text.Height > ActualHeight + 1) return;

            Geometry shape = text.BuildGeometry(new Point(0, (ActualHeight - text.Height) * .5));
            drawing.DrawGeometry(null, new Pen(Palette.Paint("#57181133"), .6), shape);

            var seam = Geometry.Combine(shape, shape.GetWidenedPathGeometry(new Pen(Brushes.Black, label.FontSize * .06)),
                GeometryCombineMode.Exclude, null);
            var thread = new Pen(Palette.Paint("#BFFFF4DE"), label.FontSize * .021) {
                DashStyle = new DashStyle(new double[] { 2, 2.5 }, 0)
            };
            drawing.PushClip(shape);
            drawing.DrawGeometry(null, thread, seam);
            drawing.Pop();
        }
    }
}
