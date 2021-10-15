using System;
using System.IO;
using System.Threading.Tasks;
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


namespace ImageList
{
    public static class UploadImage
    {
        [FunctionName("UploadImage")]
        [return: Table("Images", Connection ="StorageAccount")]
        public static async Task<ImageTable> Run(
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
    }
}
