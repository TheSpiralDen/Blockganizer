using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;


namespace Blockganizer
{
    internal sealed class Palette
    {
        public readonly Brush Background, Top, Panel, Card, Raised, Border, Text, Secondary, Accent, AccentInk, Today, Danger, Hero, TodayTag, TodayInk, Track, Complete, CompleteInk, Library, QuietLine;
        public readonly bool Light;
        public Palette(bool light)
        {
            Light = light;
            Background = Paint(light ? "#F0ECFF" : "#1A1631");
            Top = Paint(light ? "#B0F8F4FF" : "#30130F26");
            Panel = Paint(light ? "#47FFFCFF" : "#4A2F2750");
            Card = Paint(light ? "#FFFFFAFC" : "#352B52");
            Raised = Paint(light ? "#EAE3FF" : "#493B68");
            Border = Paint(light ? "#C7B9EE" : "#766698");
            Text = Paint(light ? "#18164D" : "#F7F1FF");
            Secondary = Paint(light ? "#4E4875" : "#D3C7E9");
            Accent = Paint(light ? "#2352EC" : "#B6A1FF");
            AccentInk = Paint(light ? "#FFFFFF" : "#211344");
            Today = Paint(light ? "#79FFC4C7" : "#615E3655");
            TodayTag = Paint(light ? "#B83350" : "#FFADB8");
            TodayInk = Paint(light ? "#FFFFFF" : "#34162A");
            Hero = Paint(light ? "#F0E8FF" : "#372752");
            Track = Paint(light ? "#BFFCF5FF" : "#574869");
            Complete = Paint(light ? "#33DDA1" : "#63E9B4");
            CompleteInk = Paint("#10382C");
            Library = Paint(light ? "#EFFFF9F6" : "#E622193A");

            QuietLine = Paint(light ? "#A89BD9" : "#9582B5");
            Danger = Paint(light ? "#A3264C" : "#FFADB8");
        }
        public static SolidColorBrush Paint(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }
        public Brush CategoryInk(string color)
        {
            if (!Light) return Paint(color);
            var c = (Color)ColorConverter.ConvertFromString(color);
            return new SolidColorBrush(Color.FromRgb((byte)(c.R * .35), (byte)(c.G * .35), (byte)(c.B * .35)));
        }

    }

    internal static class Ui
    {
        internal static readonly FontFamily InterfaceTypeface = new FontFamily("Trebuchet MS, Segoe UI");
        private static ImageSource artwork, wash, clockFabric;

        public static Image Artwork()
        {
            if (artwork == null) artwork = LoadImage("Blockganizer.WeekBlocks.png");
            return new Image { Source = artwork, Stretch = Stretch.Uniform, IsHitTestVisible = false };
        }

        public static Image ClockFabric()
        {
            if (clockFabric == null) clockFabric = LoadImage("Blockganizer.ClockFabric.png");
            return new Image { Source = clockFabric, Stretch = Stretch.Uniform, IsHitTestVisible = false };
        }

        public static Brush Watercolor(bool light)
        {
            if (wash == null) wash = LoadImage("Blockganizer.Watercolor.jpg");
            var brush = new ImageBrush(wash) { Stretch = Stretch.UniformToFill, Opacity = light ? .88 : .22 };
            brush.Freeze();
            return brush;
        }

        private static ImageSource LoadImage(string resource)
        {
            using (var stream = typeof(Ui).Assembly.GetManifestResourceStream(resource))
            {
                var bitmap = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                bitmap.Freeze();
                return bitmap;
            }
        }

        public static Brush DayWash(Brush start)
        {
            var color = ((SolidColorBrush)start).Color;
            var end = Color.FromArgb((byte)(color.A / 3), color.R, color.G, color.B);
            var brush = new LinearGradientBrush(color, end, new Point(0, 0), new Point(0, 1));
            brush.Freeze();
            return brush;
        }

        public static FrameworkElement DropTarget(Palette p)
        {
            var guide = Box(p.Top, p.QuietLine, 14, 9);
            guide.Margin = new Thickness(5, 7, 5, 0);
            guide.IsHitTestVisible = false;
            var content = new StackPanel();
            var arrow = Text("↓", 22, p.Accent, true);
            arrow.HorizontalAlignment = HorizontalAlignment.Center;
            content.Children.Add(arrow);
            var label = Text("Arrastra un cubo aquí", 12, p.Secondary, true);
            label.TextWrapping = TextWrapping.Wrap;
            label.TextAlignment = TextAlignment.Center;
            label.Margin = new Thickness(0, 3, 0, 2);
            content.Children.Add(label);
            guide.Child = content;
            return guide;
        }

        public static FrameworkElement Meter(int value, int max, Brush fill, Brush track, double height)
        {
            var shell = Box(track, null, height / 2);
            shell.Height = height;
            if (max <= 0 || value <= 0) return shell;
            var grid = new Grid();
            int complete = Math.Min(value, max);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(complete, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(max - complete, GridUnitType.Star) });
            Put(grid, Box(fill, null, height / 2));
            shell.Child = grid;
            return shell;
        }

        public static TextBlock Text(string text, double size, Brush color, bool strong = false)
        {
            return new TextBlock {
                Text = text, FontSize = size, Foreground = color, FontFamily = InterfaceTypeface,
                FontWeight = strong ? FontWeights.Bold : FontWeights.Normal,
                TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center
            };
        }
        public static Border Box(Brush fill, Brush stroke, double radius, double padding = 0)
        {
            return new Border { Background = fill, BorderBrush = stroke, BorderThickness = new Thickness(stroke == null ? 0 : 1),
                CornerRadius = new CornerRadius(radius), Padding = new Thickness(padding) };
        }
        public static Button Button(string label, string name, Palette p, Action click, bool primary = false, bool quiet = false)
        {
            var button = new Button {
                Content = label, Foreground = primary ? p.AccentInk : p.Text,
                Background = primary ? p.Accent : quiet ? Brushes.Transparent : p.Card,
                BorderBrush = primary || quiet ? Brushes.Transparent : p.Border,
                BorderThickness = new Thickness(1), Padding = new Thickness(11, 5, 11, 5),
                MinHeight = 31, FontSize = 12, Cursor = System.Windows.Input.Cursors.Hand
            };
            AutomationProperties.SetName(button, name);
            if (name != label) button.ToolTip = name;
            button.Click += (_, __) => click();
            var shell = new FrameworkElementFactory(typeof(Border), "Surface");
            shell.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
            shell.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(System.Windows.Controls.Button.BackgroundProperty));
            shell.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(System.Windows.Controls.Button.BorderBrushProperty));
            shell.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(System.Windows.Controls.Button.BorderThicknessProperty));
            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            presenter.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(System.Windows.Controls.Button.PaddingProperty));
            shell.AppendChild(presenter);
            var template = new ControlTemplate(typeof(Button)) { VisualTree = shell };
            AddTrigger(template, UIElement.IsMouseOverProperty, true, Border.BackgroundProperty, primary ? p.Accent : p.Raised);
            AddTrigger(template, UIElement.IsMouseOverProperty, true, Border.OpacityProperty, primary ? .85 : 1.0);
            AddTrigger(template, UIElement.IsKeyboardFocusedProperty, true, Border.BorderBrushProperty, primary ? p.Text : p.Accent);
            AddTrigger(template, UIElement.IsEnabledProperty, false, Border.OpacityProperty, .4);
            button.Template = template;
            return button;
        }
        public static ControlTemplate ComboTemplate(Palette p)
        {
            const string markup = @"
<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                 TargetType='ComboBox'>
  <Grid x:Name='Root'>
    <Border x:Name='Field' CornerRadius='7' Background='{TemplateBinding Background}'
            BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='1'/>
    <ToggleButton Focusable='False' ClickMode='Press'
                  IsChecked='{Binding IsDropDownOpen, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}'>
      <ToggleButton.Template>
        <ControlTemplate TargetType='ToggleButton'>
          <Border Background='Transparent'>
            <Path Data='M 0 0 L 4 4 L 8 0' Stroke='__INK__' StrokeThickness='1.35'
                  HorizontalAlignment='Right' VerticalAlignment='Center' Margin='0,0,10,0'/>
          </Border>
        </ControlTemplate>
      </ToggleButton.Template>
    </ToggleButton>
    <ContentPresenter x:Name='Label' Content='{TemplateBinding SelectionBoxItem}'
                      ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'
                      VerticalAlignment='Center' Margin='9,0,29,0' IsHitTestVisible='False'/>
    <TextBox x:Name='PART_EditableTextBox' Visibility='Collapsed' Background='Transparent'
             Foreground='__INK__' CaretBrush='__INK__' BorderThickness='0' Padding='3,0'
             Margin='6,1,26,1' VerticalContentAlignment='Center' IsReadOnly='{TemplateBinding IsReadOnly}'/>
    <Popup x:Name='PART_Popup' Placement='Bottom' PlacementTarget='{Binding ElementName=Root}'
           AllowsTransparency='True' IsOpen='{TemplateBinding IsDropDownOpen}'>
      <Border Background='__CARD__' BorderBrush='__LINE__' BorderThickness='1' CornerRadius='7'
              MinWidth='{Binding ActualWidth, ElementName=Root}'
              MaxHeight='{TemplateBinding MaxDropDownHeight}' Margin='0,3,0,0' Padding='3'>
        <ScrollViewer CanContentScroll='True' VerticalScrollBarVisibility='Auto'>
          <ItemsPresenter KeyboardNavigation.DirectionalNavigation='Contained'/>
        </ScrollViewer>
      </Border>
    </Popup>
  </Grid>
  <ControlTemplate.Triggers>
    <Trigger Property='IsEditable' Value='True'>
      <Setter TargetName='PART_EditableTextBox' Property='Visibility' Value='Visible'/>
      <Setter TargetName='Label' Property='Visibility' Value='Collapsed'/>
    </Trigger>
    <Trigger Property='IsKeyboardFocusWithin' Value='True'>
      <Setter TargetName='Field' Property='BorderBrush' Value='__ACCENT__'/>
    </Trigger>
  </ControlTemplate.Triggers>
</ControlTemplate>";
            return (ControlTemplate)System.Windows.Markup.XamlReader.Parse(markup
                .Replace("__INK__", p.Text.ToString()).Replace("__CARD__", p.Card.ToString())
                .Replace("__LINE__", p.Border.ToString()).Replace("__ACCENT__", p.Accent.ToString()));
        }

        private static void AddTrigger(ControlTemplate template, DependencyProperty property, bool value, DependencyProperty target, object replacement)
        {
            var trigger = new Trigger { Property = property, Value = value };
            trigger.Setters.Add(new Setter(target, replacement, "Surface"));
            template.Triggers.Add(trigger);
        }
        public static Style ScrollStyle(Palette p)
        {
            var style = new Style(typeof(ScrollBar));
            style.Setters.Add(new Setter(FrameworkElement.WidthProperty, 8.0));
            style.Setters.Add(new Setter(Control.TemplateProperty, ScrollTemplate(p, true)));
            var horizontal = new Trigger { Property = ScrollBar.OrientationProperty, Value = Orientation.Horizontal };
            horizontal.Setters.Add(new Setter(FrameworkElement.WidthProperty, double.NaN));
            horizontal.Setters.Add(new Setter(FrameworkElement.HeightProperty, 10.0));
            horizontal.Setters.Add(new Setter(Control.TemplateProperty, ScrollTemplate(p, false)));
            style.Triggers.Add(horizontal);
            return style;
        }
        private static ControlTemplate ScrollTemplate(Palette p, bool vertical)
        {
            string direction = vertical ? "Vertical" : "Horizontal";
            string reverse = vertical ? "True" : "False";
            string margin = vertical ? "2,1,1,1" : "1,3,1,1";
            string decrease = vertical ? "PageUpCommand" : "PageLeftCommand";
            string increase = vertical ? "PageDownCommand" : "PageRightCommand";
            const string repeat = "<RepeatButton Focusable='False' Command='{x:Static ScrollBar.{0}}'><RepeatButton.Template>" +
                "<ControlTemplate><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton>";
            return (ControlTemplate)System.Windows.Markup.XamlReader.Parse(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
                "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ScrollBar'>" +
                "<Track x:Name='PART_Track' Orientation='" + direction + "' IsDirectionReversed='" + reverse + "'>" +
                "<Track.DecreaseRepeatButton>" + repeat.Replace("{0}", decrease) + "</Track.DecreaseRepeatButton>" +
                "<Track.IncreaseRepeatButton>" + repeat.Replace("{0}", increase) + "</Track.IncreaseRepeatButton>" +
                "<Track.Thumb><Thumb><Thumb.Template><ControlTemplate><Border CornerRadius='4' Background='" + p.Border +
                "' Margin='" + margin + "'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb></Track></ControlTemplate>");
        }

        public static StackPanel Row() { return new StackPanel { Orientation = Orientation.Horizontal }; }
        public static void Put(Grid grid, UIElement item, int row = 0, int column = 0)
        {
            Grid.SetRow(item, row);
            Grid.SetColumn(item, column);
            grid.Children.Add(item);
        }
        public static void AutoRows(Grid grid, int count)
        {
            for (int i = 0; i < count; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }
        public static void Fade(UIElement element)
        {
            if (SystemParameters.ClientAreaAnimation)
                element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(.82, 1, TimeSpan.FromMilliseconds(140)));
        }

    }
}
