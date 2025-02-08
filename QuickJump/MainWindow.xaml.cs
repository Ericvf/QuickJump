using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnimationExtensions;
using QuickJump.ViewModels;

namespace QuickJump
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel mainViewModel;
        private bool isVisiblityToggle = false;
        HotkeyManager hotkeys;

        Animation showAnimation, hideAnimation;
        
        private void IsDeactivated()
        {
            isVisiblityToggle = true;
            hideAnimation?.Stop();
            showAnimation = LayoutRoot
                .Fade(0, 100, Eq.OutSine)
                .ThenDo(d => Hide())
            .Play();
        }

        private void IsActivated()
        {
            LayoutRoot.Opacity = 0;
            logo.Opacity = 1;

            showAnimation?.Stop();
            mainViewModel.LoadData(true);

            logo.Move(0, -200).Fade(0)
                .Fade(0.20, 300, Eq.OutSine)
                .Move(0, 0, 300, Eq.OutBack).Play();

            hideAnimation = LayoutRoot.Fade(1, 200, Eq.OutSine)
                .ThenDo(_ => Dispatcher.BeginInvoke(new Action(() => { SearchTextBox.Focus(); }), System.Windows.Threading.DispatcherPriority.Input))
                .Play();
        }

        public MainWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();
            LayoutRoot.Opacity = 0;

            Loaded += MainWindow_Loaded;
            KeyDown += MainWindow_KeyDown;
            this.Activated += (s, e) => this.IsActivated();
            this.Deactivated += (s, e) => this.IsDeactivated();
            Closed += MainWindow_Closed;
            this.mainViewModel = mainViewModel;
            DataContext = mainViewModel;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            mainViewModel.Cancel();
            hotkeys.Unregister(0);
        }

        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            Hide();
            isVisiblityToggle = true;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            hotkeys = new HotkeyManager(this);
            hotkeys.Register(0, ModifierKeys.Alt, Key.Q);
            hotkeys.Pressed += hotkeys_Pressed;

            Dispatcher.BeginInvoke(new Action(() => { SearchTextBox.Focus(); }), System.Windows.Threading.DispatcherPriority.Input);
            Task.Run(() => mainViewModel.StartBackgroundFetching());
        }

        private void hotkeys_Pressed(object sender, PressedEventArgs e)
        {
            if (isVisiblityToggle)
            {
                isVisiblityToggle = !isVisiblityToggle;
                LayoutRoot.Opacity = 0;
                Show();
                SearchTextBox.Focus();
                SearchTextBox.SelectAll();
                Activate();
            }
            else
            {
                isVisiblityToggle = !isVisiblityToggle;
                IsDeactivated();
            }

        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                hotkeys_Pressed(sender, null);
            }
        }

        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (ItemsListBox.Items.Count > 0)
                {
                    ItemsListBox.SelectedIndex = 0;
                    ItemsListBox.ScrollIntoView(ItemsListBox.Items[0]);
                    ItemsListBox.UpdateLayout();
                    var item = (ListBoxItem)ItemsListBox.ItemContainerGenerator.ContainerFromIndex(ItemsListBox.SelectedIndex);
                    item.Focus();
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (ItemsListBox.Items.Count > 0)
                {
                    ItemsListBox.SelectedIndex = ItemsListBox.Items.Count - 1;
                    ItemsListBox.ScrollIntoView(ItemsListBox.Items[ItemsListBox.Items.Count - 1]);
                    ItemsListBox.UpdateLayout();
                    var item = (ListBoxItem)ItemsListBox.ItemContainerGenerator.ContainerFromIndex(ItemsListBox.SelectedIndex);
                    item.Focus();
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

                hotkeys_Pressed(sender, null);
                mainViewModel.ExecuteItem();
                e.Handled = true;
            }
        }

        private void ItemsListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                hotkeys_Pressed(sender, null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                hotkeys_Pressed(sender, null);
                mainViewModel.ExecuteItem();
                e.Handled = true;

            }
            else if (e.Key != Key.Up && e.Key != Key.Down)
            {
                SearchTextBox.Focus();
                e.Handled = true;
            }
        }

        private void ItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemsListBox.SelectedIndex == -1)
            {
                ItemsListBox.SelectedIndex = 0;
            }
        }


    }
}