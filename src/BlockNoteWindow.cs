using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Blockganizer
{
    internal sealed class BlockNoteWindow : Window
    {
        private readonly TextBox input;
        public string Note { get { return input.Text.Trim(); } }

        public BlockNoteWindow(Window owner, Palette p, Block block)
        {
            Owner = owner;
            Title = "Nota de la actividad";
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            SizeToContent = SizeToContent.Height;
            Width = 408;
            Background = p.Background;
            FontFamily = Ui.InterfaceTypeface;

            var panel = new StackPanel { Margin = new Thickness(18) };
            var name = Ui.Text(block.Name, 21, p.Text, true);
            name.TextWrapping = TextWrapping.Wrap;
            panel.Children.Add(name);
            var date = Ui.Text(Calendar.Capital(block.Date.ToString("dddd d 'de' MMMM", Calendar.Mexican)), 12, p.Secondary);
            date.Margin = new Thickness(0, 3, 0, 10);
            panel.Children.Add(date);

            input = new TextBox {
                Text = block.Note, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxLength = 600,
                Height = 105, Padding = new Thickness(8), FontSize = 14, Foreground = FabricInk.Field(p.Text),
                FontWeight = FontWeights.Bold,
                CaretBrush = p.Text, Background = p.Card, BorderBrush = p.Border
            };
            AutomationProperties.SetName(input, "Nota de " + block.Name);
            panel.Children.Add(input);

            var actions = Ui.Row();
            actions.Margin = new Thickness(0, 12, 0, 0);
            actions.HorizontalAlignment = HorizontalAlignment.Right;
            var cancel = Ui.Button("Cancelar", "Cancelar nota", p, () => DialogResult = false);
            cancel.IsCancel = true;
            actions.Children.Add(cancel);
            var save = Ui.Button("Guardar nota", "Guardar nota", p, () => DialogResult = true, true);
            save.Margin = new Thickness(6, 0, 0, 0);
            actions.Children.Add(save);
            panel.Children.Add(actions);
            Content = panel;
            Loaded += (_, __) => { input.Focus(); input.CaretIndex = input.Text.Length; };
        }
    }
}
