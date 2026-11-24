using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Blockganizer
{
    internal sealed class BlockClock : Grid
    {
        public const double DialWidth = 78;
        private readonly Block block;
        private readonly Grid dial;
        private readonly Border pill;
        private readonly CountdownRing ring, smallRing;
        private readonly TextBlock digits, smallDigits;
        private readonly string state;
        private long lastSecond = -1;

        public BlockClock(Block block, Palette p)
        {
            this.block = block;
            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;

            state = block.Done ? "Hecho" : block.Running ? "En curso" : block.RemainingMilliseconds >= 0 ? "En pausa" : "Listo";
            Brush accent = block.Done || block.Running ? p.Complete
                : block.RemainingMilliseconds >= 0 ? Palette.Paint(p.Light ? "#B36912" : "#FFC57C")
                : null;
            Brush stateInk = accent == p.Complete ? Palette.Paint(p.Light ? "#14674E" : "#9CEBCB")
                : accent ?? p.Secondary;
            Brush dialInk = Palette.Paint("#292140");
            Brush dialState = Palette.Paint(block.Done || block.Running ? "#14674E" :
                block.RemainingMilliseconds >= 0 ? "#82480C" : "#514266");
            Brush dialAccent = block.Done || block.Running ? Palette.Paint("#197857")
                : block.RemainingMilliseconds >= 0 ? Palette.Paint("#AD641A") : null;

            dial = new Grid { Width = DialWidth, Height = DialWidth };
            Ui.Put(dial, Ui.ClockFabric());
            ring = new CountdownRing(Palette.Paint("#42FFFAEE"), Palette.Paint("#D1BFAE"), dialAccent, 2.5, block.Done, 10);
            Ui.Put(dial, ring);
            var face = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            digits = Digits(16, dialInk);
            face.Children.Add(digits);
            var caption = Ui.Text(state, 9, dialState, true);
            caption.HorizontalAlignment = HorizontalAlignment.Center;
            caption.Margin = new Thickness(0, 2, 0, 0);
            face.Children.Add(caption);
            Ui.Put(dial, face);
            Children.Add(dial);

            pill = Ui.Box(p.Card, p.Border, 13);
            pill.Padding = new Thickness(8, 4, 9, 4);
            var compact = Ui.Row();
            var badge = new Grid { Width = 19, Height = 19, VerticalAlignment = VerticalAlignment.Center };
            Ui.Put(badge, Ui.ClockFabric());
            smallRing = new CountdownRing(null, Palette.Paint("#CAB9B1"), dialAccent, 1.5, block.Done, 2);
            Ui.Put(badge, smallRing);
            compact.Children.Add(badge);
            smallDigits = Digits(12, p.Text);
            smallDigits.Margin = new Thickness(6, 0, 6, 0);
            compact.Children.Add(smallDigits);
            compact.Children.Add(Ui.Text(state, 10, stateInk, true));
            pill.Child = compact;
            Children.Add(pill);
            SetCompact(true);
        }

        public void SetCompact(bool compact)
        {
            dial.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            pill.Visibility = compact ? Visibility.Visible : Visibility.Collapsed;
        }

        public void Update(long now)
        {
            long left = block.TimeLeft(now);
            double fraction = left / (block.Minutes * 60000.0);
            ring.SetFraction(fraction);
            smallRing.SetFraction(fraction);
            long seconds = (left + 999) / 1000;
            if (seconds == lastSecond) return;
            lastSecond = seconds;
            string time = seconds >= 3600
                ? (seconds / 3600) + ":" + (seconds / 60 % 60).ToString("00") + ":" + (seconds % 60).ToString("00")
                : (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            digits.Text = smallDigits.Text = time;
            digits.FontSize = time.Length > 5 ? 11.5 : 16;
            AutomationProperties.SetName(this, block.Name + " · " + time + " · " + state);
        }

        private static TextBlock Digits(double size, Brush ink)
        {
            var text = Ui.Text("", size, ink, true);
            Typography.SetNumeralAlignment(text, FontNumeralAlignment.Tabular);
            text.HorizontalAlignment = HorizontalAlignment.Center;
            return text;
        }

        private sealed class CountdownRing : FrameworkElement
        {
            private readonly Brush fill;
            private readonly Pen track, progress;
            private readonly bool complete;
            private readonly double inset;
            private double fraction = 1;

            public CountdownRing(Brush fill, Brush track, Brush progress, double stroke, bool complete, double inset)
            {
                this.fill = fill;
                this.track = MakePen(track, stroke);
                this.progress = progress == null ? null : MakePen(progress, stroke);
                this.complete = complete;
                this.inset = inset;
                IsHitTestVisible = false;
            }

            public void SetFraction(double value)
            {
                value = Math.Max(0, Math.Min(1, value));
                if (Math.Abs(fraction - value) < .0001) return;
                fraction = value;
                InvalidateVisual();
            }

            protected override void OnRender(DrawingContext dc)
            {
                double radius = (Math.Min(ActualWidth, ActualHeight) - track.Thickness) / 2 - inset;
                if (radius <= 0) return;
                var center = new Point(ActualWidth / 2, ActualHeight / 2);
                dc.DrawEllipse(fill, track, center, radius, radius);
                if (progress == null) return;
                if (complete || fraction >= .9999)
                {
                    dc.DrawEllipse(null, progress, center, radius, radius);
                    return;
                }
                if (fraction <= 0) return;
                double angle = Math.PI * 2 * fraction;
                var end = new Point(center.X + radius * Math.Sin(angle), center.Y - radius * Math.Cos(angle));
                var arc = new StreamGeometry();
                using (var context = arc.Open())
                {
                    context.BeginFigure(new Point(center.X, center.Y - radius), false, false);
                    context.ArcTo(end, new Size(radius, radius), 0, fraction > .5, SweepDirection.Clockwise, true, false);
                }
                dc.DrawGeometry(null, progress, arc);
            }

            private static Pen MakePen(Brush brush, double stroke)
            {
                var pen = new Pen(brush, stroke) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                pen.Freeze();
                return pen;
            }
        }
    }
}
