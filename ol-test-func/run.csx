using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.MobileServices;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Azure.WebJobs.Host;

async public static Task Run(Stream myBlob, CloudBlockBlob outputBlob, string name, TraceWriter log)
{
    int width = 320;
    int height = 320;
    bool smartCropping = true;
    string _apiKey = "fddddeebb8214c0491828d49be6b6803";
    string _apiUrlBase = "https://api.projectoxford.ai/vision/v1.0/generateThumbnail";

    using (var httpClient = new HttpClient())
    {
        httpClient.BaseAddress = new Uri(_apiUrlBase);
        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
        using (HttpContent content = new StreamContent(myBlob))
        {
            //get response
            content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");
            var uri = $"{_apiUrlBase}?width={width}&height={height}&smartCropping={smartCropping.ToString()}";
            var response = httpClient.PostAsync(uri, content).Result;
            var responseBytes = response.Content.ReadAsByteArrayAsync().Result;


            //write to output thumb
            await outputBlob.UploadFromByteArrayAsync(responseBytes, 0, responseBytes.Length);
            
            MobileServiceClient client = new MobileServiceClient("http://ol-test.azurewebsites.net");
            var results = await client.GetTable<Location>().Where(x => x.Name==name).ToListAsync();
            
            Location loc = results.FirstOrDefault(); 
            loc.Image = outputBlob.StorageUri.ToString();

            await client.GetTable<Location>().UpdateAsync(loc);
        }
    } 
}

public class Location
{
    public string Id{get;set;}
    public string Image {get;set;}
    public string Name { get; set; }
}