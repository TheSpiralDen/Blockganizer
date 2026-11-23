using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace Blockganizer
{
    internal sealed class Editor : Window
    {
        public Activity Result { get; private set; }

        public Editor(Window owner, Palette p, Activity original, Activity[] catalog, DateTime week, bool duplicate)
        {
            Title = duplicate ? "Duplicar actividad" : original == null ? "Nueva actividad" : "Editar actividad";
            Owner = owner;
            Width = 470;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;
            Background = p.Background;
            Foreground = p.Text;
            FontFamily = owner.FontFamily;

            Resources[SystemColors.WindowBrushKey] = p.Card;
            Resources[SystemColors.WindowTextBrushKey] = p.Text;
            Resources[SystemColors.HighlightBrushKey] = p.Accent;
            Resources[SystemColors.HighlightTextBrushKey] = p.AccentInk;
            Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = Ui.ScrollStyle(p);

            var form = new StackPanel { Margin = new Thickness(23, 17, 23, 19) };
            Content = new ScrollViewer { Content = form, Background = p.Background, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            MaxHeight = SystemParameters.WorkArea.Height;
            var banner = Ui.Box(p.Hero, p.Border, 14, 12);
            var bannerRow = Ui.Row();
            var preview = new CubeIcon(original == null ? Catalog.Icons.Last().Value : original.Icon,
                original == null ? Catalog.Colors[0].Value : original.Color, 52);
            preview.Margin = new Thickness(0, 0, 10, 0);
            bannerRow.Children.Add(preview);
            bannerRow.Children.Add(Ui.Text(Title, 22, p.Text, true));
            banner.Child = bannerRow;
            form.Children.Add(banner);
            var name = Input(duplicate ? original.Name + " (copia)" : original == null ? "" : original.Name, p, 70);
            Field(form, "Nombre", name, p);
            var pair = Pair(form);
            var category = new ComboBox { IsEditable = true, MaxDropDownHeight = 180, MinHeight = 30,
                ItemsSource = catalog.Select(a => a.Category).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(c => c).ToArray(),
                Text = original == null ? "" : original.Category };
            Field(pair.Item1, "Categoría", category, p);
            var minutes = Input(original == null ? "30" : original.Minutes.ToString(), p, 4);
            Field(pair.Item2, "Duración en minutos", minutes, p);

            pair = Pair(form);
            var icon = new ComboBox { ItemsSource = Catalog.Icons, MinHeight = 30, MaxDropDownHeight = 260,
                SelectedItem = Catalog.Icons.FirstOrDefault(i => original != null && i.Value == original.Icon) ?? Catalog.Icons.Last() };
            Field(pair.Item1, "Icono", icon, p);
            var color = new ComboBox { ItemsSource = Catalog.Colors, MinHeight = 30, MaxDropDownHeight = 240,
                SelectedItem = Catalog.Colors.FirstOrDefault(c => original != null && c.Value == original.Color) ?? Catalog.Colors[0] };
            Field(pair.Item2, "Color", color, p);
            foreach (ComboBox combo in new[] { category, icon, color })
            {
                combo.Foreground = p.Text;
                combo.Background = p.Card;
                combo.BorderBrush = p.Border;
                combo.Template = Ui.ComboTemplate(p);
            }
            Action updatePreview = () =>
            {
                var symbol = (Choice)icon.SelectedItem;
                var tint = (Choice)color.SelectedItem;
                if (symbol == null || tint == null) return;
                preview.SetAppearance(symbol.Value, tint.Value);
            };
            icon.SelectionChanged += (_, __) => updatePreview();
            color.SelectionChanged += (_, __) => updatePreview();
            updatePreview();
            var desc = Input(original == null ? "" : original.Description, p, 400);
            desc.AcceptsReturn = true;
            desc.TextWrapping = TextWrapping.Wrap;
            desc.Height = 58;
            desc.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            Field(form, "Descripción (opcional)", desc, p);
            var goal = Input(original == null || original.Goal == 0 ? "" : original.Goal.ToString(), p, 5);
            Field(form, "Objetivo semanal en minutos (opcional)", goal, p);
            var hint = Ui.Text("Se aplica desde la semana del " + week.ToString("d 'de' MMMM 'de' yyyy", Calendar.Mexican) + ".", 11, p.Secondary);
            hint.Margin = new Thickness(0, 5, 0, 0);
            form.Children.Add(hint);
            var error = Ui.Text("", 12, p.Danger);
            error.TextWrapping = TextWrapping.Wrap;
            error.Margin = new Thickness(0, 7, 0, 0);
            error.Visibility = Visibility.Collapsed;
            form.Children.Add(error);
            var buttons = Ui.Row();
            buttons.Margin = new Thickness(0, 16, 0, 0);
            buttons.HorizontalAlignment = HorizontalAlignment.Right;
            var cancel = Ui.Button("Cancelar", "Cancelar", p, () => Close());
            cancel.IsCancel = true;
            buttons.Children.Add(cancel);
            var save = Ui.Button("Guardar actividad", "Guardar actividad", p, () =>
            {
                int length, target = 0;
                if (name.Text.Trim().Length == 0 || category.Text.Trim().Length == 0 || category.Text.Trim().Length > 35 ||
                    !int.TryParse(minutes.Text, out length) || length < 1 || length > 1440 ||
                    (goal.Text.Trim().Length > 0 && (!int.TryParse(goal.Text, out target) || target < 0 || target > 10080)))
                {
                    error.Text = "Escribe un nombre y una categoría (hasta 35 caracteres), una duración de 1 a 1440 min y un objetivo de hasta 10 080 min.";
                    error.Visibility = Visibility.Visible;
                    return;
                }
                Result = new Activity {
                    Id = original != null && !duplicate ? original.Id : 0,
                    Name = name.Text.Trim(), Category = category.Text.Trim(), Minutes = length, Goal = target,
                    Description = desc.Text.Trim(), Icon = ((Choice)icon.SelectedItem).Value, Color = ((Choice)color.SelectedItem).Value
                };
                DialogResult = true;
            }, true);
            save.IsDefault = true;
            save.Margin = new Thickness(8, 0, 0, 0);
            buttons.Children.Add(save);
            form.Children.Add(buttons);
            Loaded += (_, __) => { name.Focus(); name.SelectAll(); };
        }
        private static Tuple<StackPanel,StackPanel> Pair(Panel parent)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            var left = new StackPanel();
            var right = new StackPanel();
            Ui.Put(grid, left);
            Ui.Put(grid, right, 0, 2);
            parent.Children.Add(grid);
            return Tuple.Create(left, right);
        }
        private static TextBox Input(string text, Palette p, int max)
        {
            return new TextBox { Text = text, MaxLength = max, Background = p.Card, Foreground = p.Text,
                BorderBrush = p.Border, Padding = new Thickness(7, 4, 7, 4), MinHeight = 30, CaretBrush = p.Text };
        }
        private static void Field(Panel parent, string title, Control input, Palette p)
        {
            var label = Ui.Text(title, 12, p.Secondary, true);
            label.Margin = new Thickness(0, 10, 0, 4);
            parent.Children.Add(label);
            AutomationProperties.SetName(input, title);
            parent.Children.Add(input);
        }
    }
}