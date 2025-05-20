using Azure.Data.Tables;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using TableStorageDemo.Desktop.Model;

namespace TableStorageDemo.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string TABLE_NAME = "OfficeSupplies";
        private readonly string connectionString;
        private readonly TableServiceClient serviceClient;
        private readonly TableClient tableClient;
        private readonly ObservableCollection<OfficeSupply> items = new ObservableCollection<OfficeSupply>();

        public MainWindow()
        {
            InitializeComponent();

            connectionString = Properties.Settings.Default.StorageConnectionString;
            serviceClient = new TableServiceClient(connectionString);
            serviceClient.CreateTableIfNotExists(TABLE_NAME);
            tableClient = serviceClient.GetTableClient(TABLE_NAME);

            dataGrid.ItemsSource = items;
            GetItems();

            items.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var items = e.OldItems;
                if (items != null)
                {
                    foreach (var i in items)
                    {
                        var item = (OfficeSupply)i;
                        tableClient.DeleteEntity(item.PartitionKey, item.RowKey);
                    }
                }
            }
        }

        private void GetItems()
        {
            var entities = tableClient.Query<OfficeSupply>();
            items.Clear();
            foreach (var item in entities)
            {
                item.Edited += OnItemEdited;
                items.Add(item);
            }
        }

        private void OnRowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit
                && e.Row.IsNewItem
                && e.Row.Item is OfficeSupply item)
            {
                item.RowKey = Guid.NewGuid().ToString();
                item.PartitionKey = "1";
                item.Edited += OnItemEdited;
            }
        }

        private void OnItemEdited(object? sender, EventArgs e)
        {
            if (sender is OfficeSupply item)
            {
                tableClient.UpsertEntity(item);
            }
        }
    }
}
