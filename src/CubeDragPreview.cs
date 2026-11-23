using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Blockganizer
{
    internal sealed class CubeDragPreview : Adorner, IDisposable
    {
        private readonly AdornerLayer layer;
        private readonly CubeButton source;
        private readonly BitmapSource image;
        private readonly Size size;
        private readonly Point grip;
        private readonly TranslateTransform translation;
        private readonly RotateTransform rotation;

        private CubeDragPreview(FrameworkElement area, CubeButton source, FrameworkElement cube, Point grip, AdornerLayer layer) : base(area)
        {
            this.layer = layer;
            this.source = source;
            var scale = source.LayoutTransform.Value;
            this.grip = scale.Transform(grip);
            Size original = cube.RenderSize;
            size = new Size(original.Width * scale.M11, original.Height * scale.M22);
            translation = new TranslateTransform(-size.Width, -size.Height);
            rotation = new RotateTransform(0, this.grip.X, this.grip.Y);
            IsHitTestVisible = false;
            Opacity = .88;
            ClipToBounds = true;

            var dpi = VisualTreeHelper.GetDpi(cube);
            var snapshot = new RenderTargetBitmap((int)Math.Ceiling(original.Width * dpi.DpiScaleX),
                (int)Math.Ceiling(original.Height * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            snapshot.Render(cube);
            snapshot.Freeze();
            image = snapshot;

            layer.Add(this);
            DragDrop.AddPreviewGiveFeedbackHandler(source, OnDragFeedback);
            FollowCursor();
            if (SystemParameters.ClientAreaAnimation) StartRotation();
        }

        public static CubeDragPreview Start(FrameworkElement area, CubeButton source, Point grip)
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(area);
            var cube = (FrameworkElement)source.Content;
            return layer == null || cube.ActualWidth <= 0 || cube.ActualHeight <= 0
                ? null : new CubeDragPreview(area, source, cube, grip, layer);
        }

        private void OnDragFeedback(object sender, GiveFeedbackEventArgs e)
        {
            FollowCursor();
        }

        private void FollowCursor()
        {
            CursorPoint mouse;
            if (PresentationSource.FromVisual(AdornedElement) != null && GetCursorPos(out mouse))
                PositionAt(AdornedElement.PointFromScreen(new Point(mouse.X, mouse.Y)));
        }

        internal void PositionAt(Point pointer)
        {
            translation.X = pointer.X - grip.X;
            translation.Y = pointer.Y - grip.Y;
        }

        private void StartRotation()
        {
            var ease = new SineEase { EasingMode = EasingMode.EaseInOut };
            var tilt = new DoubleAnimationUsingKeyFrames {
                Duration = TimeSpan.FromMilliseconds(1200), RepeatBehavior = RepeatBehavior.Forever
            };
            tilt.KeyFrames.Add(new EasingDoubleKeyFrame(8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)), ease));
            tilt.KeyFrames.Add(new EasingDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(900)), ease));
            tilt.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1200)), ease));
            rotation.BeginAnimation(RotateTransform.AngleProperty, tilt);
        }

        protected override void OnRender(DrawingContext drawing)
        {
            drawing.PushTransform(translation);
            drawing.PushTransform(rotation);
            drawing.DrawImage(image, new Rect(size));
            drawing.Pop();
            drawing.Pop();
        }

        public void Dispose()
        {
            DragDrop.RemovePreviewGiveFeedbackHandler(source, OnDragFeedback);
            rotation.BeginAnimation(RotateTransform.AngleProperty, null);
            layer.Remove(this);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CursorPoint
        {
            public int X, Y;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out CursorPoint point);
    }
}
