using QuickJump.Providers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace QuickJump.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly ObservableCollection<Item> items;
        private readonly IEnumerable<IItemsProvider> itemsProviders;

        public ICollectionView FilteredItems { get; }

        private bool isLoading;
        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                if (isLoading != value)
                {
                    isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        private int itemsCount;
        public int ItemsCount
        {
            get { return itemsCount; }
            set
            {
                if (itemsCount != value)
                {
                    itemsCount = value;
                    OnPropertyChanged(nameof(ItemsCount));
                }
            }
        }

        private int filteredItemsCount;
        public int FilteredItemsCount
        {
            get { return filteredItemsCount; }
            set
            {
                if (filteredItemsCount != value)
                {
                    filteredItemsCount = value;
                    OnPropertyChanged(nameof(FilteredItemsCount));
                }
            }
        }

        private bool hasFilteredItems;
        public bool HasFilteredItems
        {
            get { return hasFilteredItems; }
            set
            {
                if (hasFilteredItems != value)
                {
                    hasFilteredItems = value;
                    OnPropertyChanged(nameof(HasFilteredItems));
                }
            }
        }

        private string filterText = default;
        private string[] filterKeywords = new string[0];

        public string FilterText
        {
            get => filterText;
            set
            {
                if (filterText != value)
                {
                    filterText = value;
                    filterKeywords = filterText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    OnPropertyChanged(nameof(FilterText));
                    UpdateView();
                }
            }
        }

        private Item selectedItem;
        public Item SelectedItem
        {
            get => selectedItem;
            set
            {
                if (selectedItem != value)
                {
                    selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));
                }
            }
        }

        public MainViewModel(IEnumerable<IItemsProvider> itemsProviders)
        {
            this.itemsProviders = itemsProviders;
            items = new ObservableCollection<Item>();
            FilteredItems = CollectionViewSource.GetDefaultView(items);
            FilteredItems.Filter = FilterItems;
        }

        public void UpdateView()
        {
            FilteredItems.Refresh();
            if (FilteredItems is ListCollectionView l)
            {
                HasFilteredItems = l.Count > 0;
                FilteredItemsCount = l.Count;
                ItemsCount = items.Count;
            }
        }

        public async Task StartBackgroundFetching()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                await LoadData(false);

                await Task.Delay(1000 * 60 * 15, cancellationTokenSource.Token);
            }
        }

        public async Task LoadData(bool isOnActivate)
        {
            if (!IsLoading)
            {
                IsLoading = true;

                await Task.WhenAll(itemsProviders
                    .Where(i => !isOnActivate || i.LoadDataOnActivate)
                    .Select(async itemsProvider =>
                {
                    try
                    {
                        Debug.WriteLine($"Started: {itemsProvider.Name}");
                        var stopwatch = Stopwatch.StartNew();
                        var existingItemsMap = items.Where(i => i.Provider == itemsProvider.Name).ToDictionary(i => i.Id);
                        var fetchedNames = new HashSet<string>();

                        await itemsProvider.GetItems(async item =>
                        {
                            fetchedNames.Add(item.Id);

                            if (existingItemsMap.TryGetValue(item.Id, out var existingItem))
                            {
                                existingItem.Name = item.Name;
                            }
                            else
                            {
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    items.Add(item);
                                    UpdateView();
                                }, DispatcherPriority.Normal, cancellationTokenSource.Token);
                            }
                        }, cancellationTokenSource.Token);

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            for (int i = existingItemsMap.Keys.Count - 1; i >= 0; i--)
                            {
                                if (!fetchedNames.Contains(existingItemsMap[existingItemsMap.Keys.ElementAt(i)].Id))
                                {
                                    items.RemoveAt(i);
                                }
                            }

                            UpdateView();
                        }, DispatcherPriority.Normal, cancellationTokenSource.Token);

                        Debug.WriteLine($"Done: {itemsProvider.Name}, {stopwatch.ElapsedMilliseconds}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception: {ex?.Message.ToString()}");
                    }
                }));

                IsLoading = false;
            }
        }

        private bool FilterItems(object obj)
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                return false;
            }

            if (obj is Item item)
            {
                var itemField = item.Description ?? item.Name;
                foreach (var keyword in filterKeywords)
                {
                    if (keyword.StartsWith("!"))
                    {
                        var negatedKeyword = keyword.Substring(1);
                        if (negatedKeyword.Length > 0 && itemField.Contains(negatedKeyword, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!itemField.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public void ExecuteItem()
        {
            if (SelectedItem != null)
            {
                switch (SelectedItem.Type)
                {
                    case Types.File:
                        Process.Start(
                            new ProcessStartInfo(SelectedItem.Id)
                            {
                                UseShellExecute = true
                            });
                        break;
                    case Types.ProcessId:
                        var windowHandle = Convert.ToInt32(SelectedItem.Path);
                        ShowWindow(windowHandle, SW_RESTORE);
                        SetForegroundWindow(windowHandle);
                        break;

                    default:
                    case Types.Uri:
                        Process.Start(
                            new ProcessStartInfo(SelectedItem.Path ?? $"http://google.com?q={SelectedItem.Name}")
                            {
                                UseShellExecute = true
                            });
                        break;

                }
            }
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }

        private const int SW_RESTORE = 9;
        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(int hwnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}