using QuickJump.Services;
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
        private readonly IItemsService itemsService;

        public ICollectionView FilteredItems { get; }

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
        public string FilterText
        {
            get => filterText;
            set
            {
                if (filterText != value)
                {
                    filterText = value;
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

        public MainViewModel(IItemsService itemsService)
        {
            this.itemsService = itemsService;
            items = new ObservableCollection<Item>();
            FilteredItems = CollectionViewSource.GetDefaultView(items);
            FilteredItems.Filter = FilterItems;
        }

        public void UpdateView()
        {
            FilteredItems.Refresh();
            HasFilteredItems = FilteredItems is ListCollectionView l && l.Count > 0;
        }

        public async Task StartBackgroundFetching()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                await LoadData(cancellationTokenSource.Token);

                await Application.Current.Dispatcher.InvokeAsync(() => UpdateView());

                await Task.Delay(1000 * 60 * 60, cancellationTokenSource.Token);
            }
        }

        public async Task LoadData(CancellationToken cancellationToken)
        {
            var retrievedItems = await itemsService.GetAllItems();

            var existingItemsMap = items.ToDictionary(i => i.Id);
            var fetchedNames = new HashSet<string>(retrievedItems.Select(f => f.Id));

            foreach (var newItem in retrievedItems)
            {
                if (existingItemsMap.TryGetValue(newItem.Id, out var existingItem))
                {
                    existingItem.Name = newItem.Name;
                }
                else
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => items.Add(newItem));
                }
            }

            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (!fetchedNames.Contains(items[i].Id))
                {
                    await Dispatcher.CurrentDispatcher.InvokeAsync(() => items.RemoveAt(i));
                }
            }
        }

        private bool FilterItems(object obj)
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                return true;
            }

            if (obj is Item item)
            {
                return item.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
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
    }
}