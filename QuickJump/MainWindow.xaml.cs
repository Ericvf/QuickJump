using AnimationExtensions;
using QuickJump.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace QuickJump
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel mainViewModel;
        private bool isVisiblityToggle = true;
        private Animation showAnimation, hideAnimation;
        private HotkeyManager hotkeys;

        public MainWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();

            LayoutRoot.Hide();

            Loaded += MainWindow_Loaded;
            Activated += (s, e) => this.IsActivated();
            Deactivated += (s, e) => this.IsDeactivated();
            KeyDown += MainWindow_KeyDown;
            MouseDown += MainWindow_MouseDown;
            Closed += MainWindow_Closed;

            this.mainViewModel = mainViewModel;
            DataContext = mainViewModel;

            var loadingStarted = refresh
                .Fade(0).Fade(1, 500)
                .Rotate(0).Rotate(270, 1500, Eq.OutCubic);

            var loadingEnded = refresh.Fade(1).Fade(0, 500);

            mainViewModel.LoadingStarted += () => Dispatcher.BeginInvoke(() => loadingStarted.Play());
            mainViewModel.LoadingEnded += () => Dispatcher.BeginInvoke(() => loadingEnded.Play());
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TrayIconHelper.AddTrayIcon(new WindowInteropHelper(this).Handle, "QuickJump v1.0 - appbyfex");

            hotkeys = new HotkeyManager(this);
            hotkeys.Register(0, ModifierKeys.Alt, Key.Q);
            hotkeys.Pressed += ToggleVisibility;
            hotkeys.IconClicked += Hotkeys_IconClicked;

            Dispatcher.BeginInvoke(new Action(() => { SearchTextBox.Focus(); }), System.Windows.Threading.DispatcherPriority.Input);
            Task.Run(() => mainViewModel.StartBackgroundFetching());
            ItemsListBox.SelectedIndex = 0;
        }

        private void Hotkeys_IconClicked(object sender, ButtonEventArgs e)
        {
            if (e.IsRight)
            {
                IsActivated();
            }
            else
            {
                IsActivated();
            }
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                IsDeactivated();
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            mainViewModel.Cancel();
            hotkeys.Unregister(0);
        }

        private async void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                var index = ItemsListBox.SelectedIndex;
                if (ItemsListBox.Items.Count > 0)
                {
                    ItemsListBox.UpdateLayout();
                    var item = (ListBoxItem)ItemsListBox.ItemContainerGenerator.ContainerFromIndex(index);
                    item?.Focus();
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (ItemsListBox.Items.Count > 0)
                {
                    ItemsListBox.SelectedIndex = ItemsListBox.Items.Count - 1;
                    ItemsListBox.ScrollIntoView(ItemsListBox.Items[^1]);
                    ItemsListBox.UpdateLayout();
                    var item = (ListBoxItem)ItemsListBox.ItemContainerGenerator.ContainerFromIndex(ItemsListBox.SelectedIndex);
                    item?.Focus();
                }

                e.Handled = true;

            }
            else if (e.Key == Key.Enter)
            {
                if (mainViewModel.FilterText?.Equals("exit", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Close();
                    e.Handled = true;
                    return;
                }

                if (ItemsListBox.SelectedItem == null && ItemsListBox.Items.Count > 0)
                {
                    ItemsListBox.SelectedIndex = 0;
                    ItemsListBox.ScrollIntoView(ItemsListBox.Items[0]);
                    ItemsListBox.UpdateLayout();
                }

                ToggleVisibility(sender, null);
                await mainViewModel.ExecuteItem();
                e.Handled = true;
            }
        }

        private async void ItemsListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                IsDeactivated();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                ToggleVisibility(sender, null);
                await mainViewModel.ExecuteItem();
                e.Handled = true;

            }
            else if (e.Key != Key.Up && e.Key != Key.Down)
            {
                SearchTextBox.Focus();
                e.Handled = true;
            }
        }

        private async void ItemsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ToggleVisibility(sender, null);
            await mainViewModel.ExecuteItem();
            e.Handled = true;
        }

        private void ItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemsListBox.SelectedIndex == -1)
            {
                ItemsListBox.SelectedIndex = 0;
            }
        }

        private void ToggleVisibility(object sender, PressedEventArgs e)
        {
            if (isVisiblityToggle)
            {
                IsActivated();
            }
            else
            {
                IsDeactivated();
            }
        }

        private void IsDeactivated()
        {
            if (!isVisiblityToggle)
            {
                isVisiblityToggle = true;
                hideAnimation?.Stop();
                showAnimation?.Stop();
                showAnimation = LayoutRoot
                    .Fade(0, 300, Eq.OutSine)
                    .Scale(0.50, 0.50, 200, Eq.InBack)
                    .ThenDo(d => Hide())
                .Play();
            }
        }

        private void IsActivated()
        {
            if (isVisiblityToggle)
            {
                isVisiblityToggle = false;
                Show();
                Activate();
                SearchTextBox.Focus();
                SearchTextBox.SelectAll();


                showAnimation?.Stop();
                hideAnimation?.Stop();

                hideAnimation = LayoutRoot.Fade(0).Fade(1, 200, Eq.OutSine)
                    .Scale(1, 1, 200, Eq.OutBack)
                    .ThenDo(_ => Dispatcher.BeginInvoke(new Action(() => { SearchTextBox.Focus(); }), System.Windows.Threading.DispatcherPriority.Input))
                    .Play();

                logo.Move(0, -200).Fade(0)
                    .Fade(0.20, 500, Eq.OutSine)
                    .Move(0, 0, 1000, Eq.OutElastic).Play();

                mainViewModel.LoadData(true);
            }
        }
    }
}