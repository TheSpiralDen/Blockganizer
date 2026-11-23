using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Blockganizer
{
    internal sealed class CubeButton : Button
    {
        private readonly CubeIcon cube;
        private readonly DropShadowEffect glow;
        private readonly Action<Point> beginDrag;
        private Point start;
        private bool armed, dropHighlight;

        public CubeButton(string symbol, string color, double size, string name, string duration, string description, bool completed,
            Palette palette, Action<Point> beginDrag)
        {
            this.beginDrag = beginDrag;
            Width = Height = size;
            Cursor = Cursors.Hand;
            FocusVisualStyle = null;
            ToolTip = description;
            ToolTipService.SetInitialShowDelay(this, 320);
            AutomationProperties.SetName(this, description.Replace("\n", " · "));
            AutomationProperties.SetHelpText(this, "Enter o clic derecho para abrir las opciones");

            cube = new CubeIcon(symbol, color, size);
            cube.ShowActivity(name, duration);
            if (completed) cube.ShowCompleted();
            glow = new DropShadowEffect {
                Color = ((SolidColorBrush)palette.Accent).Color, ShadowDepth = 0, BlurRadius = 14, Opacity = 0
            };
            cube.Effect = glow;
            Content = cube;
            Template = new ControlTemplate(typeof(Button)) { VisualTree = new FrameworkElementFactory(typeof(ContentPresenter)) };
            MouseEnter += (_, __) => UpdateGlow();
            MouseLeave += (_, __) => UpdateGlow();
            IsKeyboardFocusedChanged += (_, __) => UpdateGlow();
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters parameters)
        {
            Point point = parameters.HitPoint;
            return cube.ContainsSurface(point) ? new PointHitTestResult(this, point) : null;
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            e.Handled = true;
            start = e.GetPosition(this);
            if (!cube.ContainsSurface(start)) return;
            Focus();
            armed = beginDrag != null && CaptureMouse();
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            e.Handled = true;
            armed = false;
            if (IsMouseCaptured) ReleaseMouseCapture();
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            armed = false;
            base.OnLostMouseCapture(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!armed || e.LeftButton != MouseButtonState.Pressed) return;
            Point current = e.GetPosition(this);
            if (Math.Abs(current.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(current.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            armed = false;
            ReleaseMouseCapture();
            e.Handled = true;
            beginDrag(start);
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            Focus();
            OnClick();
            e.Handled = true;
        }

        public void SetDropHighlight(bool highlighted)
        {
            dropHighlight = highlighted;
            UpdateGlow();
        }

        private void UpdateGlow()
        {
            glow.Opacity = dropHighlight || IsKeyboardFocused ? .92 : IsMouseOver ? .48 : 0;
        }
    }
}
