using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using System.Windows.Threading;
using System.Windows.Shell;

namespace Blockganizer
{
    internal sealed class MainWindow : Window
    {
        private readonly Store store;
        private DateTime week = Calendar.Monday(DateTime.Today);
        private Palette p;
        private string category, notice = "";
        private int quoteOffset;
        private bool archived;
        private List<Activity> activities;
        private List<Block> blocks;
        private TextBlock status;
        private ScrollViewer boardScroll, libraryScroll;
        private Grid viewport, surface;
        private FrameworkElement previewTarget;
        private Action clearPreview;
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        private long timerDeadline;
        private bool draggingCube;
        private BlockClock activeClock;
        private readonly Dictionary<DateTime, ScrollViewer> dayScrolls = new Dictionary<DateTime, ScrollViewer>();
        private readonly Dictionary<string, Button> focusTargets = new Dictionary<string, Button>();
        private const string DragFormat = "Blockganizer.Block";
        private static readonly string[] Days = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
        private static readonly string[] ShortDays = { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };

        public MainWindow(Store data)
        {
            store = data;
            Title = "Blockganizer";
            Width = 1320;
            Height = 850;
            MinWidth = 760;
            MinHeight = 575;
            WindowStyle = WindowStyle.None;
            WindowChrome.SetWindowChrome(this, new WindowChrome {
                CaptionHeight = 42, GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(0),
                ResizeBorderThickness = SystemParameters.WindowResizeBorderThickness
            });
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = Ui.InterfaceTypeface;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            CommandBindings.Add(new CommandBinding(ApplicationCommands.New, (_, __) => Edit(null, false)));
            InputBindings.Add(new KeyBinding(ApplicationCommands.New, Key.N, ModifierKeys.Control));
            StateChanged += (_, __) => Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(UpdateWorkArea));
            LocationChanged += (_, __) => UpdateWorkArea();
            timer.Tick += (_, __) => UpdateCountdown();
            Closed += (_, __) => timer.Stop();
            Render();
        }

        private void Render(bool newWeek = false, string focus = null)
        {
            double boardX = boardScroll == null ? 0 : boardScroll.HorizontalOffset;
            double libraryX = libraryScroll == null ? 0 : libraryScroll.HorizontalOffset;
            var offsets = dayScrolls.ToDictionary(pair => pair.Key, pair => pair.Value.VerticalOffset);
            dayScrolls.Clear();
            focusTargets.Clear();
            activeClock = null;
            store.CompleteExpiredTimers(Now());
            timerDeadline = store.TimerDeadline();
            timer.IsEnabled = timerDeadline > 0;
            p = new Palette(store.Setting("theme", "light") == "light");
            Background = p.Background;
            Foreground = p.Text;
            Resources[typeof(ScrollBar)] = Ui.ScrollStyle(p);
            activities = store.Activities(week);
            blocks = store.Week(week);
            var root = new Grid { Background = p.Background };
            Ui.AutoRows(root, 2);
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Ui.AutoRows(root, 2);
            viewport = new Grid { Background = p.Background };
            surface = root;
            Ui.Put(viewport, new AdornerDecorator { Child = root });
            viewport.SizeChanged += (_, __) => UpdateWorkArea();
            viewport.Loaded += (_, __) => UpdateWorkArea();
            Content = viewport;
            var backdrop = new Border { Background = Ui.Watercolor(p.Light), IsHitTestVisible = false };
            Grid.SetRowSpan(backdrop, 5);
            Ui.Put(root, backdrop);
            Ui.Put(root, Header());
            Ui.Put(root, WeekHeading(), 1);
            Ui.Put(root, Board(), 2);
            Ui.Put(root, Library(), 3);
            Ui.Put(root, Footer(), 4);
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                boardScroll.ScrollToHorizontalOffset(boardX);
                libraryScroll.ScrollToHorizontalOffset(libraryX);
                foreach (var pair in offsets)
                {
                    ScrollViewer scroll;
                    if (!newWeek && dayScrolls.TryGetValue(pair.Key, out scroll))
                        scroll.ScrollToVerticalOffset(pair.Value);
                }
                Button target;
                if (focus != null && focusTargets.TryGetValue(focus, out target)) target.Focus();
            }));
            if (newWeek) Ui.Fade(boardScroll);
        }

        private void UpdateWorkArea()
        {
            if (viewport == null) return;
            Thickness padding = DesktopWorkArea.PaddingFor(this, viewport);
            if (surface.Margin != padding) surface.Margin = padding;
        }

        private FrameworkElement Header()
        {
            var bar = Ui.Box(p.Top, null, 0);
            bar.Height = 48;
            var grid = new Grid { Margin = new Thickness(22, 0, 10, 0) };
            bar.Child = grid;
            var identity = Ui.Row();
            identity.VerticalAlignment = VerticalAlignment.Center;
            var emblem = Ui.Artwork();
            emblem.Width = emblem.Height = 32;
            identity.Children.Add(emblem);
            var title = Ui.Text("Blockganizer", 22, p.Text, true);
            title.FontWeight = FontWeights.Bold;
            title.Margin = new Thickness(8, 0, 0, 0);
            identity.Children.Add(title);
            Ui.Put(grid, identity);
            var actions = Ui.Row();
            actions.HorizontalAlignment = HorizontalAlignment.Right;
            actions.VerticalAlignment = VerticalAlignment.Center;
            WindowChrome.SetIsHitTestVisibleInChrome(actions, true);
            Button settings = null;
            settings = Ui.Button("⚙", "Configuración", p, () =>
            {
                var menu = NewMenu();
                Item(menu, p.Light ? "Activar modo oscuro" : "Activar modo claro", () =>
                    Change(() => store.SetSetting("theme", p.Light ? "dark" : "light"), "Apariencia guardada", "settings"));
                Open(menu, settings);
            });
            focusTargets["settings"] = settings;
            actions.Children.Add(settings);
            var minimize = Ui.Button("−", "Minimizar", p, () => SystemCommands.MinimizeWindow(this), quiet: true);
            minimize.Margin = new Thickness(6, 0, 0, 0);
            actions.Children.Add(minimize);
            var maximize = Ui.Button("□", "Maximizar o restaurar", p, () =>
            {
                if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
                else SystemCommands.MaximizeWindow(this);
            }, quiet: true);
            actions.Children.Add(maximize);
            actions.Children.Add(Ui.Button("×", "Cerrar", p, Close, quiet: true));
            Ui.Put(grid, actions);
            return bar;
        }

        private FrameworkElement WeekHeading()
        {
            var hero = Ui.Box(null, null, 0);
            hero.Margin = new Thickness(24, 8, 22, 14);
            hero.MinHeight = 100;
            var grid = new Grid();
            Ui.AutoRows(grid, 2);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            hero.Child = grid;

            var lead = new Grid { VerticalAlignment = VerticalAlignment.Center };
            lead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            lead.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            lead.ColumnDefinitions.Add(new ColumnDefinition());
            var stripe = Ui.Box(p.TodayTag, null, 4);
            stripe.Width = 6;
            stripe.Height = 60;
            stripe.Margin = new Thickness(0, 0, 13, 0);
            Ui.Put(lead, stripe);

            var title = new StackPanel();
            title.Children.Add(Ui.Text("Semana " + Calendar.Week(week).ToString("00"), 15, p.Text, true));
            var range = Ui.Text(Calendar.Range(week), 35, p.Text, true);
            range.FontWeight = FontWeights.Bold;
            title.Children.Add(range);
            Ui.Put(lead, title, 0, 1);

            var summary = new StackPanel { Margin = new Thickness(22, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            if (blocks.Count == 0)
            {
                summary.Children.Add(Ui.Text("Semana sin actividades", 12, p.Secondary));
            }
            else
            {
                int completed = blocks.Count(b => b.Done);
                var meter = Ui.Meter(completed, blocks.Count, p.Complete, p.Track, 7);
                meter.Width = 112;
                meter.HorizontalAlignment = HorizontalAlignment.Left;
                summary.Children.Add(meter);
                var progress = Ui.Text(completed + " de " + blocks.Count + " completadas", 12, p.Text, true);
                progress.Margin = new Thickness(0, 5, 0, 0);
                summary.Children.Add(progress);
            }
            Ui.Put(lead, summary, 0, 2);
            Ui.Put(grid, lead);
            var quote = QuotePanel();
            Ui.Put(grid, quote, 0, 1);

            var art = Ui.Artwork();
            art.Width = art.Height = 100;
            art.Margin = new Thickness(6, 0, 14, 0);
            Ui.Put(grid, art, 0, 2);
            var actions = Ui.Row();
            actions.VerticalAlignment = VerticalAlignment.Center;
            AddButton(actions, "‹", "Semana anterior", () => Navigate(week.AddDays(-7), "previous"), "previous");
            var todayButton = AddButton(actions, "Hoy", "Hoy", () => Navigate(Calendar.Monday(DateTime.Today), "today"), "today");
            todayButton.Background = p.Complete;
            todayButton.Foreground = p.CompleteInk;
            AddButton(actions, "›", "Semana siguiente", () => Navigate(week.AddDays(7), "next"), "next");
            Button goals = null;
            goals = AddButton(actions, "◎  Objetivos", "Objetivos semanales", () => ShowGoals(goals), "goals");
            Ui.Put(grid, actions, 0, 3);
            grid.SizeChanged += (_, e) =>
            {
                double width = e.NewSize.Width;
                bool narrow = width < 1180;
                art.Visibility = width >= 1000 ? Visibility.Visible : Visibility.Collapsed;
                summary.Visibility = width >= 790 ? Visibility.Visible : Visibility.Collapsed;
                range.FontSize = width >= 960 ? 35 : 24;
                Grid.SetRow(quote, narrow ? 1 : 0);
                Grid.SetColumn(quote, narrow ? 0 : 1);
                Grid.SetColumnSpan(quote, narrow ? 4 : 1);
                quote.Margin = narrow ? new Thickness(0, 8, 0, 0) : new Thickness(20, 0, 20, 0);
            };
            return hero;
        }

        private FrameworkElement QuotePanel()
        {
            var panel = Ui.Box(p.Top, null, 17);
            panel.MaxWidth = 580;
            panel.VerticalAlignment = VerticalAlignment.Center;
            var layout = new Grid();
            panel.Child = layout;
            var words = new StackPanel { Margin = new Thickness(39, 10, 39, 10) };
            var line = Ui.Text("", 16, p.Text);
            line.FontStyle = FontStyles.Italic;
            line.TextAlignment = TextAlignment.Center;
            line.TextWrapping = TextWrapping.Wrap;
            var author = Ui.Text("", 11, p.Secondary, true);
            author.Margin = new Thickness(0, 4, 0, 0);
            author.TextAlignment = TextAlignment.Center;
            words.Children.Add(line);
            words.Children.Add(author);
            Ui.Put(layout, words);

            Action show = () =>
            {
                var quote = MotivationalQuotes.ForDay(DateTime.Today, quoteOffset);
                line.Text = "«" + quote.Text + "»";
                author.Text = quote.Author;
                words.ToolTip = quote.Source;
            };
            var next = Ui.Button("↻", "Otra frase", p, () =>
            {
                quoteOffset = (quoteOffset + 1) % MotivationalQuotes.Count;
                show();
            }, quiet: true);
            next.Width = 32;
            next.Padding = new Thickness(0);
            next.HorizontalAlignment = HorizontalAlignment.Right;
            next.VerticalAlignment = VerticalAlignment.Center;
            next.Margin = new Thickness(0, 0, 5, 0);
            Ui.Put(layout, next);
            show();
            return panel;
        }
        private Button AddButton(Panel row, string label, string name, Action action, string focus = null, bool primary = false)
        {
            var button = Ui.Button(label, name, p, action, primary);
            button.Margin = new Thickness(6, 0, 0, 0);
            row.Children.Add(button);
            if (focus != null) focusTargets[focus] = button;
            return button;
        }
        private void Navigate(DateTime target, string focus)
        {
            week = target;
            notice = "";
            Render(true, focus);
        }

        private FrameworkElement Board()
        {
            boardScroll = new ScrollViewer { Margin = new Thickness(25, 0, 25, 13),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalContentAlignment = HorizontalAlignment.Stretch };
            var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Left };
            boardScroll.Content = grid;
            boardScroll.SizeChanged += (_, __) => grid.Width = Math.Max(1225, boardScroll.ActualWidth);
            for (int i = 0; i < 7; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                DateTime date = week.AddDays(i);
                Ui.Put(grid, Day(date, ShortDays[i], i == 6), 0, i);
            }
            return boardScroll;
        }

        private FrameworkElement Day(DateTime date, string name, bool last)
        {
            bool today = date == DateTime.Today;
            var list = blocks.Where(b => b.Date == date).ToArray();
            var frame = Ui.Box(Ui.DayWash(today ? p.Today : p.Panel), Brushes.Transparent, 20);
            frame.Margin = new Thickness(0, 0, last ? 0 : 8, 0);
            var layout = new Grid();
            Ui.AutoRows(layout, 1);
            layout.RowDefinitions.Add(new RowDefinition());
            frame.Child = layout;

            var head = Ui.Box(today ? p.Today : p.Top, null, 19);
            head.Height = 60;
            head.Margin = new Thickness(0, 0, 0, 8);
            head.Padding = new Thickness(11, 0, 10, 0);
            var dayLabel = new Grid();
            Ui.AutoRows(dayLabel, 2);
            dayLabel.VerticalAlignment = VerticalAlignment.Center;
            var caption = Ui.Text(name + " " + date.Day, 21, p.Text, true);
            caption.FontWeight = FontWeights.Bold;
            Ui.Put(dayLabel, caption);
            if (today)
            {
                var tag = Ui.Box(p.TodayTag, null, 10);
                tag.Padding = new Thickness(7, 3, 7, 3);
                tag.HorizontalAlignment = HorizontalAlignment.Right;
                tag.VerticalAlignment = VerticalAlignment.Center;
                tag.Child = Ui.Text("Hoy", 10, p.TodayInk, true);
                Ui.Put(dayLabel, tag);
            }
            int minutes = list.Sum(b => b.Minutes);
            string amount = minutes == 0 ? "0 h" : Calendar.Duration(minutes);
            var duration = Ui.Text(amount + " · Planificado", 11, p.Secondary, minutes > 0);
            duration.Margin = new Thickness(0, 1, 0, 0);
            AutomationProperties.SetName(duration, "Tiempo planificado " + amount);
            Ui.Put(dayLabel, duration, 1);
            head.Child = dayLabel;
            Ui.Put(layout, head);

            var body = new StackPanel { Margin = new Thickness(6, 0, 6, 6) };
            var scroll = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            DateTime suggestedDay = week == Calendar.Monday(DateTime.Today) ? DateTime.Today : week;
            if (blocks.Count == 0 && date == suggestedDay) body.Children.Add(Ui.DropTarget(p));
            for (int i = 0; i < list.Length; i++) body.Children.Add(BlockCube(list, i, scroll, body.Margin.Left + body.Margin.Right));
            dayScrolls[date] = scroll;
            Ui.Put(layout, scroll, 1);
            Brush dayWash = frame.Background;
            AcceptDrop(frame, date, null, position =>
            {
                frame.BorderBrush = position.HasValue ? p.Accent : Brushes.Transparent;
                frame.Background = position.HasValue ? p.Hero : dayWash;
            });
            return frame;
        }

        private FrameworkElement BlockCube(Block[] list, int index, ScrollViewer scroll, double dayPadding)
        {
            const double cubeSize = 124, compactCubeSize = 110, clockGap = 5;
            Block block = list[index];
            string key = "block" + block.Id;
            Action complete = () => Change(() => store.Complete(block.Id, !block.Done),
                block.Done ? "Actividad pendiente" : "Actividad completada", key);
            var cube = CreateCube(block.Icon, block.Color, cubeSize, block.Name, block.Minutes, block.Description,
                block.Done, new DragItem { Id = block.Id, Priority = block.Priority });
            cube.HorizontalAlignment = HorizontalAlignment.Left;
            cube.Margin = new Thickness(0, 0, 0, 5);
            focusTargets[key] = cube;

            cube.Click += (_, __) =>
            {
                var menu = NewMenu();
                Item(menu, block.Done ? "Marcar como pendiente" : "Marcar como completada", complete);
                if (index > 0 && list[index - 1].Priority == block.Priority)
                    Item(menu, "Subir en el día", () => Move(block, block.Date, list[index - 1].Id, false, true));
                if (index + 1 < list.Length && list[index + 1].Priority == block.Priority)
                    Item(menu, "Bajar en el día", () => Move(block, block.Date, list[index + 1].Id, true, true));
                Item(menu, "Mover a", null, submenu =>
                {
                    for (int i = 0; i < 7; i++)
                    {
                        DateTime date = week.AddDays(i);
                        if (date != block.Date) Item(submenu, Days[i], () => Move(block, date, null, false, true));
                    }
                });
                Item(menu, "Quitar de este día", () => Change(() => store.Remove(block.Id), "Bloque retirado del día",
                    list.Length > 1 ? "block" + list[index == 0 ? 1 : index - 1].Id : "today"));
                Open(menu, cube);
            };
            var slot = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
            slot.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            slot.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            slot.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Ui.AutoRows(slot, 2);
            Ui.Put(slot, cube);
            var controls = BlockControls(block);
            controls.Width = 29;
            Ui.Put(slot, controls, 0, 1);

            var clock = new BlockClock(block, p);
            clock.Update(Now());
            if (block.Running) activeClock = clock;
            Ui.Put(slot, clock, 1);
            Action<double> layoutBlock = available =>
            {
                bool compact = available < cubeSize + controls.Width + BlockClock.DialWidth + 2 * clockGap;
                double size = compact ? Math.Min(compactCubeSize, Math.Max(1, available - controls.Width)) : cubeSize;
                double scale = size / cubeSize;
                if (cube.LayoutTransform.Value.M11 != scale) cube.LayoutTransform = new ScaleTransform(scale, scale);
                clock.SetCompact(compact);
                Grid.SetColumn(clock, compact ? 0 : 2);
                Grid.SetRow(clock, compact ? 1 : 0);
                Grid.SetColumnSpan(clock, compact ? 3 : 1);
                clock.Margin = compact ? new Thickness(0, -4, 0, 7) : new Thickness(clockGap, 0, clockGap, 0);
            };
            layoutBlock(compactCubeSize + controls.Width);
            scroll.ScrollChanged += (_, e) =>
            {
                if (e.ViewportWidthChange != 0 && e.ViewportWidth > dayPadding)
                    layoutBlock(e.ViewportWidth - dayPadding);
            };
            var insertion = Ui.Box(p.Accent, null, 2);
            insertion.Height = 3;
            insertion.Width = 86;
            insertion.HorizontalAlignment = HorizontalAlignment.Center;
            insertion.Visibility = Visibility.Hidden;
            insertion.IsHitTestVisible = false;
            Grid.SetColumnSpan(insertion, 3);
            Ui.Put(slot, insertion);
            AcceptDrop(cube, block.Date, block.Id, position =>
            {
                cube.SetDropHighlight(position.HasValue);
                insertion.VerticalAlignment = position == DropPosition.After ? VerticalAlignment.Bottom : VerticalAlignment.Top;
                insertion.Visibility = position.HasValue ? Visibility.Visible : Visibility.Hidden;
            }, block.Priority);
            return slot;
        }

        private FrameworkElement BlockControls(Block block)
        {
            var rail = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
            string noteKey = "note" + block.Id;
            bool hasNote = !string.IsNullOrEmpty(block.Note);
            string noteAction = hasNote ? "Ver o editar nota" : "Agregar nota";
            var note = BlockControl(hasNote ? "▣" : "▤", noteAction, block, noteKey, () =>
            {
                var editor = new BlockNoteWindow(this, p, block);
                if (editor.ShowDialog() == true)
                    Change(() => store.SetNote(block.Id, editor.Note),
                        editor.Note.Length == 0 ? "Nota eliminada" : "Nota guardada", noteKey);
            });
            if (hasNote)
            {
                note.Background = p.Raised;
                note.Foreground = p.Accent;
                note.ToolTip = new TextBlock { Text = block.Note, MaxWidth = 260, TextWrapping = TextWrapping.Wrap };
            }
            rail.Children.Add(note);

            string timerKey = "timer" + block.Id;
            string timerAction = block.Running ? "Pausar temporizador" :
                block.RemainingMilliseconds >= 0 && !block.Done ? "Reanudar temporizador" : "Iniciar temporizador";
            var playback = BlockControl(block.Running ? "⏸" : "▶", timerAction, block, timerKey,
                () => Change(() => store.ToggleTimer(block.Id, Now()),
                    block.Running ? "Temporizador en pausa" : "Temporizador iniciado", timerKey));
            playback.IsEnabled = !block.Done;
            if (block.Running)
            {
                playback.Background = p.Complete;
                playback.Foreground = p.CompleteInk;
            }
            rail.Children.Add(playback);

            string starKey = "priority" + block.Id;
            var star = BlockControl(block.Priority ? "★" : "☆",
                block.Priority ? "Quitar prioridad" : "Marcar prioridad", block, starKey,
                () => Change(() => store.SetPriority(block.Id, !block.Priority),
                    block.Priority ? "Prioridad retirada" : "Prioridad marcada", starKey));
            if (block.Priority)
            {
                star.Background = Palette.Paint("#FFDE74");
                star.Foreground = Palette.Paint("#704B09");
            }
            rail.Children.Add(star);
            return rail;
        }

        private Button BlockControl(string symbol, string action, Block block, string key, Action click)
        {
            var button = Ui.Button(symbol, action + " · " + block.Name, p, click);
            button.FontFamily = new FontFamily("Segoe UI Symbol");
            button.FontSize = 15;
            button.Width = 29;
            button.MinHeight = 27;
            button.Padding = new Thickness(0);
            button.Margin = new Thickness(0, 1, 0, 1);
            button.ToolTip = action;
            focusTargets[key] = button;
            return button;
        }

        private void UpdateCountdown()
        {
            long now = Now();
            if (!draggingCube && timerDeadline > 0 && now >= timerDeadline)
            {
                try
                {
                    notice = "Tiempo terminado · Actividad completada";
                    Render();
                }
                catch (Exception error) { Report(error); }
            }
            else if (activeClock != null) activeClock.Update(now);
        }

        private static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }

        private FrameworkElement Library()
        {
            var shell = Ui.Box(p.Library, null, 23);
            shell.Margin = new Thickness(16, 0, 16, 0);
            shell.Padding = new Thickness(13, 11, 13, 5);
            var grid = new Grid();
            Ui.AutoRows(grid, 2);
            shell.Child = grid;
            var heading = new Grid { Margin = new Thickness(0, 0, 0, 9) };
            var labels = Ui.Row();
            var mark = Ui.Box(p.Accent, null, 3);
            mark.Width = 5;
            mark.Height = 24;
            mark.VerticalAlignment = VerticalAlignment.Center;
            mark.Margin = new Thickness(0, 0, 10, 0);
            labels.Children.Add(mark);
            var dockTitle = Ui.Text("Biblioteca de actividades", 22, p.Text, true);
            dockTitle.FontWeight = FontWeights.Bold;
            labels.Children.Add(dockTitle);
            int count = activities.Count(a => a.Status == (archived ? 1 : 0));
            var summary = Ui.Text(count + (archived ? " archivadas" : count == 1 ? " actividad" : " actividades"), 11, p.Secondary);
            summary.Margin = new Thickness(9, 2, 0, 0);
            labels.Children.Add(summary);
            Ui.Put(heading, labels);
            var actions = Ui.Row();
            actions.HorizontalAlignment = HorizontalAlignment.Right;
            Button filters = null;
            filters = AddButton(actions, category ?? "Todas", "Filtrar por categoría", () =>
            {
                var menu = NewMenu();
                Item(menu, "Todas las categorías", () => { category = null; Render(false, "filter"); });
                foreach (string value in activities.Where(a => a.Status == (archived ? 1 : 0)).Select(a => a.Category)
                    .Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(c => c))
                {
                    string selected = value;
                    Item(menu, selected, () => { category = selected; Render(false, "filter"); });
                }
                Open(menu, filters);
            }, "filter");
            filters.MaxWidth = 160;
            if (!archived)
            {
                filters.Background = p.Complete;
                filters.Foreground = p.CompleteInk;
            }
            var archiveButton = AddButton(actions, archived ? "Ver activas" : "Archivadas",
                archived ? "Ver actividades activas" : "Ver actividades archivadas",
                () => { archived = !archived; category = null; Render(false, "archive"); }, "archive");
            archiveButton.Background = p.Raised;
            heading.SizeChanged += (_, e) => summary.Visibility = e.NewSize.Width >= 820 ? Visibility.Visible : Visibility.Collapsed;
            AddButton(actions, "+ Nueva actividad", "Nueva actividad", () => Edit(null, false), "new", true);
            Ui.Put(heading, actions);
            Ui.Put(grid, heading);

            var strip = Ui.Row();
            var visible = activities.Where(a => a.Status == (archived ? 1 : 0) &&
                (category == null || string.Equals(a.Category, category, StringComparison.CurrentCultureIgnoreCase))).ToArray();
            if (visible.Length == 0)
            {
                var empty = Ui.Text(archived ? "Aquí aparecen las actividades archivadas." :
                    category == null ? "Crea una actividad para empezar a planear." : "Esta categoría está vacía.", 13, p.Secondary);
                empty.Margin = new Thickness(0, 16, 0, 12);
                strip.Children.Add(empty);
            }
            foreach (Activity a in visible) strip.Children.Add(ActivityCube(a));
            strip.HorizontalAlignment = HorizontalAlignment.Center;
            var tray = new Grid();
            Ui.Put(tray, strip);
            libraryScroll = new ScrollViewer { Content = tray, MinHeight = 155,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
            var scroll = libraryScroll;
            scroll.ScrollChanged += (_, e) =>
            {
                if (e.ViewportWidthChange != 0) tray.MinWidth = e.ViewportWidth;
            };
            libraryScroll.PreviewMouseWheel += (_, e) =>
            {
                if (libraryScroll.ScrollableWidth <= 0) return;
                libraryScroll.ScrollToHorizontalOffset(libraryScroll.HorizontalOffset - e.Delta);
                e.Handled = true;
            };
            Ui.Put(grid, libraryScroll, 1);
            return shell;
        }

        private FrameworkElement ActivityCube(Activity a)
        {
            var cube = CreateCube(a.Icon, a.Color, 146, a.Name, a.Minutes, a.Description, false,
                a.Status == 0 ? new DragItem { Id = a.Id, Template = true } : null);
            cube.Margin = new Thickness(1, 0, 7, 0);
            focusTargets["activity" + a.Id] = cube;
            cube.Click += (_, __) =>
            {
                var menu = NewMenu();
                if (a.Status == 0)
                {
                    Item(menu, "Agregar a", null, submenu =>
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            DateTime date = week.AddDays(i);
                            Item(submenu, Days[i], () => Plan(a.Id, date, null, false, true));
                        }
                    });
                }
                Item(menu, "Editar", () => Edit(a, false));
                Item(menu, "Duplicar", () => Edit(a, true));
                Item(menu, a.Status == 0 ? "Archivar" : "Restaurar", () => Change(() => store.SetStatus(a.Id, a.Status == 0 ? 1 : 0),
                    a.Status == 0 ? "Actividad archivada" : "Actividad restaurada", "archive"));
                Item(menu, "Eliminar de la biblioteca", () =>
                {
                    if (MessageBox.Show(this, "¿Eliminar «" + a.Name + "» de la biblioteca? Los bloques ya planeados se conservan.",
                        "Eliminar actividad", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                        Change(() => store.SetStatus(a.Id, 2), "Actividad eliminada de la biblioteca", "new");
                });
                Open(menu, cube);
            };
            return cube;
        }

        private CubeButton CreateCube(string icon, string color, double size, string name, int minutes,
            string description, bool completed, DragItem item)
        {
            string duration = Calendar.Duration(minutes);
            string tooltip = name + " · " + duration;
            if (completed) tooltip += " · Completada";
            if (!string.IsNullOrEmpty(description)) tooltip += "\n" + description;
            CubeButton cube = null;
            Action<Point> beginDrag = null;
            if (item != null)
            {
                beginDrag = grip =>
                {
                    draggingCube = true;
                    try
                    {
                        using (CubeDragPreview.Start(surface, cube, grip))
                            DragDrop.DoDragDrop(cube, new DataObject(DragFormat, item),
                                item.Template ? DragDropEffects.Copy : DragDropEffects.Move);
                    }
                    finally
                    {
                        draggingCube = false;
                        ClearDropPreview();
                        if (timerDeadline > 0 && Now() >= timerDeadline) UpdateCountdown();
                    }
                };
            }
            cube = new CubeButton(icon, color, size, name, duration, tooltip, completed, p, beginDrag);
            return cube;
        }

        private FrameworkElement Footer()
        {
            var bar = Ui.Box(null, null, 0);
            var grid = new Grid { Margin = new Thickness(25, 4, 25, 7) };
            status = Ui.Text(notice.Length == 0 ? "●  Guardado en este dispositivo" : "●  " + notice, 11, p.Accent);
            AutomationProperties.SetLiveSetting(status, System.Windows.Automation.AutomationLiveSetting.Polite);
            Ui.Put(grid, status);
            var hint = Ui.Text("Clic derecho o Enter para ver opciones", 11, p.Secondary);
            hint.HorizontalAlignment = HorizontalAlignment.Right;
            Ui.Put(grid, hint);
            bar.Child = grid;
            return bar;
        }

        private void ShowGoals(Button target)
        {
            var popup = new Popup { PlacementTarget = target, Placement = PlacementMode.Bottom,
                StaysOpen = false, AllowsTransparency = true, PopupAnimation = SystemParameters.ClientAreaAnimation ? PopupAnimation.Fade : PopupAnimation.None };
            var box = Ui.Box(p.Panel, p.Border, 12, 15);
            box.Width = 355;
            var stack = new StackPanel();
            box.Child = stack;
            var head = new Grid();
            Ui.Put(head, Ui.Text("Objetivos · Semana " + Calendar.Week(week), 17, p.Text, true));
            var close = Ui.Button("×", "Cerrar objetivos", p, () => popup.IsOpen = false, quiet: true);
            close.HorizontalAlignment = HorizontalAlignment.Right;
            Ui.Put(head, close);
            stack.Children.Add(head);
            var entries = new StackPanel();
            var list = activities.Where(a => a.Goal > 0 && (a.Status == 0 || blocks.Any(b => b.ActivityId == a.Id))).ToArray();
            if (list.Length == 0)
            {
                var empty = Ui.Text("Agrega un objetivo en el editor de una actividad para ver aquí tus minutos completados.", 13, p.Secondary);
                empty.TextWrapping = TextWrapping.Wrap;
                empty.Margin = new Thickness(0, 7, 0, 3);
                entries.Children.Add(empty);
            }
            foreach (Activity a in list)
            {
                int completed = blocks.Where(b => b.ActivityId == a.Id && b.Done).Sum(b => b.Minutes);
                var caption = Ui.Text(a.Name, 13, p.Text, true);
                caption.Margin = new Thickness(0, 10, 0, 3);
                entries.Children.Add(caption);
                var progress = new ProgressBar { Height = 7, Minimum = 0, Maximum = a.Goal, Value = Math.Min(a.Goal, completed),
                    Background = p.Raised, Foreground = p.CategoryInk(a.Color), BorderThickness = new Thickness(0) };
                AutomationProperties.SetName(progress, a.Name + ": " + completed + " de " + a.Goal + " minutos");
                entries.Children.Add(progress);
                var value = Ui.Text(completed + " / " + a.Goal + " min", 11, p.Secondary);
                value.Margin = new Thickness(0, 3, 0, 0);
                entries.Children.Add(value);
            }
            stack.Children.Add(new ScrollViewer { Content = entries, MaxHeight = 300, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            popup.Child = box;
            box.PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { popup.IsOpen = false; e.Handled = true; } };
            popup.Closed += (_, __) => target.Focus();
            popup.IsOpen = true;
            close.Focus();
        }

        private void Edit(Activity original, bool duplicate)
        {
            var editor = new Editor(this, p, original, activities.Where(a => a.Status != 2).ToArray(), week, duplicate);
            if (editor.ShowDialog() != true) return;
            if (original == null || duplicate) archived = false;
            category = null;
            long id = 0;
            try
            {
                id = store.Save(editor.Result, week);
                notice = "Actividad guardada";
                Render(false, "activity" + id);
            }
            catch (Exception error) { Report(error); }
        }

        private void Change(Action mutation, string message, string focus = null)
        {
            try
            {
                mutation();
                notice = message;
                Render(false, focus);
            }
            catch (Exception error) { Report(error); }
        }
        private void Report(Exception error)
        {
            status.Text = "No se pudo guardar el cambio";
            status.Foreground = p.Danger;
            status.ToolTip = error.Message;
        }
        private void Plan(long id, DateTime date, long? anchor, bool after, bool keyboard)
        {
            Change(() => store.Plan(id, date, anchor, after), "Agregada al " + date.ToString("dddd", Calendar.Mexican),
                keyboard ? "activity" + id : null);
        }
        private void Move(Block b, DateTime date, long? anchor, bool after, bool keyboard)
        {
            Change(() => store.Move(b.Id, date, anchor, after), "Orden de la semana guardado", keyboard ? "block" + b.Id : null);
        }

        [Serializable]
        private sealed class DragItem
        {
            public long Id;
            public bool Template, Priority;
        }

        private enum DropPosition { Day, Before, After }

        private void ClearDropPreview()
        {
            if (clearPreview != null) clearPreview();
            previewTarget = null;
            clearPreview = null;
        }

        private void AcceptDrop(FrameworkElement surface, DateTime date, long? anchor, Action<DropPosition?> highlight,
            bool? priority = null)
        {
            surface.AllowDrop = true;
            surface.DragOver += (_, e) =>
            {
                var item = e.Data.GetData(DragFormat) as DragItem;
                if (item != null && priority.HasValue && item.Priority != priority.Value) return;
                e.Effects = item == null || (!item.Template && item.Id == anchor) ? DragDropEffects.None :
                    item.Template ? DragDropEffects.Copy : DragDropEffects.Move;
                if (previewTarget != surface) ClearDropPreview();
                if (e.Effects == DragDropEffects.None) ClearDropPreview();
                else
                {
                    previewTarget = surface;
                    clearPreview = () => highlight(null);
                    highlight(!anchor.HasValue ? DropPosition.Day :
                        e.GetPosition(surface).Y > surface.ActualHeight / 2 ? DropPosition.After : DropPosition.Before);
                }
                e.Handled = true;
            };
            surface.DragLeave += (_, e) =>
            {
                if (previewTarget == surface)
                {
                    ClearDropPreview();
                    e.Handled = true;
                }
            };
            surface.Drop += (_, e) =>
            {
                var item = e.Data.GetData(DragFormat) as DragItem;
                if (item != null && priority.HasValue && item.Priority != priority.Value) return;
                ClearDropPreview();
                e.Handled = true;
                if (item == null || (!item.Template && item.Id == anchor)) return;
                bool after = e.GetPosition(surface).Y > surface.ActualHeight / 2;
                if (item.Template) Plan(item.Id, date, anchor, after, false);
                else
                {
                    Block block = blocks.FirstOrDefault(b => b.Id == item.Id);
                    if (block != null) Move(block, date, anchor, after, false);
                }
            };
        }

        private ContextMenu NewMenu()
        {
            return new ContextMenu { Background = p.Card, Foreground = p.Text, BorderBrush = p.Border };
        }
        private void Item(ItemsControl menu, string title, Action action, Action<MenuItem> fill = null)
        {
            var item = new MenuItem { Header = title, Background = p.Card, Foreground = p.Text };
            if (action != null) item.Click += (_, __) => action();
            if (fill != null) fill(item);
            menu.Items.Add(item);
        }
        private static void Open(ContextMenu menu, Button button)
        {
            menu.PlacementTarget = button;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
            if (menu.Items.Count > 0) ((MenuItem)menu.Items[0]).Focus();
        }
    }
}
