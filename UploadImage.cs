using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ImageList
{
    public static class UploadImage
    {
        [FunctionName("UploadImage")]
        public static async Task<IActionResult> Run(
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

            return new OkObjectResult("OK");
        }
    }
}
