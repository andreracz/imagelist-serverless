using Microsoft.Azure.Cosmos.Table;

namespace ImageList
{
    public class ImageTable
    {
        public string PartitionKey { get; set; }
        public string RowKey {get; set;}
        public string Title {get;set;}
        public string Extension {get;set;}
        
    }
}