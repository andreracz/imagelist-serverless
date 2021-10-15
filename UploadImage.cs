using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Microsoft.Azure.Cosmos.Table;


namespace ImageList
{
    public static class UploadImage
    {
        [FunctionName("UploadImage")]
        [return: Table("Images", Connection ="StorageAccount")]
        public static async Task<ImageTable> UploadImageFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
            IBinder binder)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<UploadImageModel>(requestBody);
            
            var file = Convert.FromBase64String(data.File);
            var extension = data.Extension;
            var guid = Guid.NewGuid().ToString();

            var blob = new BlobAttribute("images/" + guid + "." + extension, FileAccess.Write);
            blob.Connection = "StorageAccount";
            using(var output = binder.Bind<Stream>(blob)){
                await output.WriteAsync(file, 0, file.Length);
            }

            return new ImageTable{ PartitionKey = extension, RowKey=guid, Title=data.Title, Extension=extension};
        }



        [FunctionName("GerarThumbnail")]
        public static void GerarThumbnail(
            [BlobTrigger("images/{name}", Connection ="StorageAccount")] Stream fullImage,
            [Blob("thumbnails/{name}", FileAccess.Write, Connection = "StorageAccount")] Stream thumbnail,
            ILogger log) {

            IImageFormat format;

            using (Image<Rgba32> input = Image.Load<Rgba32>(fullImage, out format))
            {
                var Height = input.Height;
                var Width = input.Width;
                var NewHeight = 100;
                double ratio = Height / 100.0;
                int NewWidth = (int) (Width / (ratio));
                input.Mutate(x => x.Resize(NewWidth, NewHeight));
                input.Save(thumbnail, format);
            }

        }

        [FunctionName("GetImages")]
        public static IActionResult GetImages(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Table("Images", Connection ="StorageAccount")] CloudTable cloudTable,
            ILogger log
            ) {
            var images = cloudTable.ExecuteQuery(new TableQuery());
            var imagesToReturn = new List<GetImageModel>();
            foreach(var image in images) {
                var props = image.Properties;
                var extension = image.Properties.ContainsKey("Extension")? image.Properties["Extension"]?.StringValue: "";
                var title = image.Properties.ContainsKey("Title")? image.Properties["Title"]?.StringValue: "";
                
                imagesToReturn.Add(new GetImageModel { 
                        Guid = image.RowKey, 
                        Extension = extension,
                        Title = title,
                    });
            }
            return new OkObjectResult(imagesToReturn);
        }

    }
}
