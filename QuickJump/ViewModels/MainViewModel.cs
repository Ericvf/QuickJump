using QuickJump.Providers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
        private readonly IItemLauncher itemLauncher;

        public ICollectionView FilteredItems { get; }

        public event Action LoadingStarted;
        public event Action LoadingEnded;
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

                    if (isLoading)
                    {
                        LoadingStarted?.Invoke();
                    }
                    else
                    {
                        LoadingEnded?.Invoke();
                    }
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

        public MainViewModel(IEnumerable<IItemsProvider> itemsProviders, IItemLauncher itemLauncher)
        {
            this.itemsProviders = itemsProviders;
            this.itemLauncher = itemLauncher;
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
                        var existingItemsMap = items.ToArray().Where(i => i.Provider == itemsProvider.Name).ToDictionary(i => i.Id);
                        var fetchedNames = new HashSet<string>();
                        var addedNames = new HashSet<string>();

                        await itemsProvider.GetItems(async item =>
                        {
                            fetchedNames.Add(item.Id);

                            if (existingItemsMap.TryGetValue(item.Id, out var existingItem))
                            {
                                existingItem.Name = item.Name;
                            }
                            else
                            {
                                addedNames.Add(item.Id);

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
                                var existingItem = existingItemsMap[existingItemsMap.Keys.ElementAt(i)];
                                if (!fetchedNames.Contains(existingItem.Id))
                                {
                                    items.Remove(existingItem);
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

        public async Task ExecuteItem()
        {
            if (SelectedItem != null)
            {
                await itemLauncher.LaunchItem(SelectedItem);
            }
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }
    }
}