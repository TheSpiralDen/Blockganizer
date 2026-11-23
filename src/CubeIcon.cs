using System;
using System.Collections.Generic;
using System.Windows.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Blockganizer
{
    internal sealed class CubeIcon : Grid
    {
        private const double PlacardWidth = 68;
        private const double PlacardHeight = 54;
        private const double TitleFontSize = 14.4;
        private static readonly Dictionary<Color, BitmapSource> Tinted = new Dictionary<Color, BitmapSource>();
        private static FormatConvertedBitmap material;
        private static byte[] neutralPixels;
        private static int stride;

        private readonly Image image;
        private readonly TextBlock glyph;
        private readonly Path instrument;
        private readonly Grid emblem;

        public CubeIcon(string symbol, string color, double size)
        {
            Width = Height = size;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Center;
            IsHitTestVisible = false;
            image = new Image { Stretch = Stretch.Uniform };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            Children.Add(image);
            glyph = Ui.Text("", size * .24, Brushes.Black, true);
            glyph.FontFamily = new FontFamily("Segoe UI Symbol");
            emblem = new Grid {
                Width = size * .28, Height = size * .28,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(size * .14, size * .11, 0, 0),
                RenderTransformOrigin = new Point(.5, .5),
                RenderTransform = new SkewTransform(0, -4)
            };
            glyph.HorizontalAlignment = HorizontalAlignment.Center;
            instrument = new Path { Stretch = Stretch.Uniform };
            emblem.Children.Add(glyph);
            emblem.Children.Add(instrument);
            Children.Add(emblem);
            SetAppearance(symbol, color);
        }

        public void SetAppearance(string symbol, string color)
        {
            var tint = (Color)ColorConverter.ConvertFromString(color);
            image.Source = Tint(tint);
            glyph.Text = symbol;
            var ink = new SolidColorBrush(Color.FromRgb(
                (byte)(tint.R * .18), (byte)(tint.G * .23), (byte)(tint.B * .24)));
            glyph.Foreground = ink;
            instrument.Fill = ink;
            instrument.Data = InstrumentGlyphs.For(symbol);
            glyph.Visibility = instrument.Data == null ? Visibility.Visible : Visibility.Collapsed;
            instrument.Visibility = instrument.Data == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public bool ContainsSurface(Point point)
        {
            if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height) return false;
            int x = (int)(point.X * material.PixelWidth / Width);
            int y = (int)(point.Y * material.PixelHeight / Height);
            return neutralPixels[y * stride + x * 4 + 3] > 128;
        }

        public void ShowActivity(string name, string duration)
        {
            double size = Width;
            emblem.Width = emblem.Height = size * .145;
            emblem.Margin = new Thickness(0, size * .11, size * .58, 0);
            emblem.RenderTransform = new SkewTransform(0, 15);
            glyph.FontSize = size * .135;
            var ink = Palette.Paint("#2A211E");
            var patch = new Grid {
                Width = PlacardWidth,
                Height = PlacardHeight,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(size * .145, size * .11, 0, 0),
                RenderTransformOrigin = new Point(.5, .5),
                RenderTransform = new SkewTransform(0, -4)
            };
            patch.Children.Add(Ui.Box(Palette.Paint("#FFF3D9"), Palette.Paint("#BA9771"), 3));
            patch.Children.Add(new Rectangle {
                Stroke = Palette.Paint("#BCA07B"), StrokeThickness = .65,
                StrokeDashArray = new DoubleCollection { 2, 2 }, Margin = new Thickness(2),
                RadiusX = 2, RadiusY = 2
            });
            var face = new StackPanel { Margin = new Thickness(4, 4, 4, 4), VerticalAlignment = VerticalAlignment.Center };
            var typeface = new Typeface(Ui.InterfaceTypeface, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            double width = patch.Width - face.Margin.Left - face.Margin.Right;
            double fontSize = TitleFontSize;
            List<string> lines = null;
            for (int attempt = 0; attempt < 7; attempt++)
            {
                fontSize = TitleFontSize - attempt * .65;
                lines = WrapActivity(name, width, typeface, fontSize, ink);
                if (lines.Count <= 2 && lines.TrueForAll(line => MeasureText(line, typeface, fontSize, ink) <= width)) break;
            }
            if (lines.Count > 2)
            {
                var words = new List<string>(name.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
                words.RemoveAll(word => string.Equals(word, "de", StringComparison.CurrentCultureIgnoreCase) ||
                    string.Equals(word, "del", StringComparison.CurrentCultureIgnoreCase));
                var compact = WrapActivity(string.Join(" ", words), width, typeface, fontSize, ink);
                if (compact.Count > 2 && words.Count > 2 &&
                    (string.Equals(words[0], "Estudiar", StringComparison.CurrentCultureIgnoreCase) ||
                     string.Equals(words[0], "Practicar", StringComparison.CurrentCultureIgnoreCase)))
                {
                    words.RemoveAt(0);
                    compact = WrapActivity(Calendar.Capital(string.Join(" ", words)), width, typeface, fontSize, ink);
                }
                if (compact.Count > 0 && compact.Count <= 2)
                {
                    lines = compact;
                    double longest = 0;
                    foreach (string line in lines)
                        longest = Math.Max(longest, MeasureText(line, typeface, fontSize, ink));
                    if (longest > width - 1)
                        fontSize = Math.Max(8, fontSize * (width - 1) / longest);
                }
            }
            if (lines.Count > 2)
            {
                lines.RemoveRange(2, lines.Count - 2);
                lines[1] += "…";
            }
            foreach (string line in lines)
            {
                var title = Ui.Text(line, fontSize, ink, true);
                title.FontFamily = typeface.FontFamily;
                title.FontWeight = FontWeights.Bold;
                title.TextAlignment = TextAlignment.Center;
                title.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
                title.LineHeight = fontSize * 1.08;
                face.Children.Add(title);
            }
            AutomationProperties.SetName(face, name);
            var time = Ui.Text(duration, 10.5, Palette.Paint("#654A37"));
            time.TextAlignment = TextAlignment.Center;
            time.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            time.LineHeight = 12;
            time.Margin = new Thickness(0, 2, 0, 0);
            face.Children.Add(time);
            patch.Children.Add(face);
            Children.Add(patch);
        }

        private static List<string> WrapActivity(string name, double width, Typeface typeface, double fontSize, Brush ink)
        {
            var lines = new List<string>();
            string line = "";
            foreach (string word in name.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && MeasureText(candidate, typeface, fontSize, ink) > width)
                {
                    lines.Add(line);
                    line = word;
                }
                else line = candidate;
            }
            if (line.Length > 0) lines.Add(line);
            return lines;
        }

        private static double MeasureText(string text, Typeface typeface, double size, Brush ink)
        {
            return new FormattedText(text, Calendar.Mexican, FlowDirection.LeftToRight, typeface, size, ink, 1).Width;
        }

        public void ShowCompleted()
        {
            var check = Ui.Text("✓", Width * .16, Palette.Paint("#17634E"), true);
            check.HorizontalAlignment = HorizontalAlignment.Center;
            check.Margin = new Thickness(Width * .10, 0, 0, Width * .53);
            Children.Add(check);
        }

        private static BitmapSource Tint(Color color)
        {
            BitmapSource bitmap;
            if (Tinted.TryGetValue(color, out bitmap)) return bitmap;
            if (material == null) LoadMaterial();
            var pixels = (byte[])neutralPixels.Clone();
            double center = (color.R + color.G + color.B) / 3.0;
            byte red = Saturate(color.R, center);
            byte green = Saturate(color.G, center);
            byte blue = Saturate(color.B, center);
            for (int i = 0; i < pixels.Length; i += 4)
            {
                if (pixels[i + 3] == 0) continue;
                double light = (pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3.0;
                pixels[i] = LitChannel(blue, light);
                pixels[i + 1] = LitChannel(green, light);
                pixels[i + 2] = LitChannel(red, light);
            }
            bitmap = BitmapSource.Create(material.PixelWidth, material.PixelHeight, 96, 96,
                PixelFormats.Bgra32, null, pixels, stride);
            bitmap.Freeze();
            Tinted[color] = bitmap;
            return bitmap;
        }

        private static byte Saturate(byte channel, double center)
        {
            return (byte)Math.Max(0, Math.Min(255, center + (channel - center) * 1.45));
        }

        private static byte LitChannel(byte channel, double light)
        {
            const double neutral = 224;
            return light <= neutral
                ? (byte)(channel * light / neutral)
                : (byte)(channel + (255 - channel) * (light - neutral) / (255 - neutral));
        }

        private static void LoadMaterial()
        {
            using (var png = typeof(CubeIcon).Assembly.GetManifestResourceStream("Blockganizer.ActivityCube.png"))
            {
                var source = BitmapFrame.Create(png, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                material = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
                material.Freeze();
            }
            stride = material.PixelWidth * 4;
            neutralPixels = new byte[stride * material.PixelHeight];
            material.CopyPixels(neutralPixels, stride, 0);
        }
    }
}
