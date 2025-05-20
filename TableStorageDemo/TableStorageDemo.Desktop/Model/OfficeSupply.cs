using Azure;
using Azure.Data.Tables;
using System;
using System.ComponentModel;

namespace TableStorageDemo.Desktop.Model
{
    internal class OfficeSupply : ITableEntity, IEditableObject
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public double Price { get; set; }
        public int Quantity { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public event EventHandler? Edited;

        public void BeginEdit() { }

        public void CancelEdit() { }

        public void EndEdit()
        {
            Edited?.Invoke(this, EventArgs.Empty);
        }
    }
}
