using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Autodesk.ADN.Toolkit.ViewData;
using Autodesk.ADN.Toolkit.ViewData.DataContracts;
using DropnWatch.Models;
using Newtonsoft.Json;

namespace DropnWatch.Controllers
{
    public class HomeController : Controller
    {
        private static readonly string BUCKET_NAME = "DropnWatchBucket109";

        static AdnViewDataClient _viewDataClient = 
            new AdnViewDataClient(
                UserSettings.BASE_URL,
                UserSettings.CONSUMER_KEY,
                UserSettings.CONSUMER_SECRET);

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [ActionName("Credentials")]
        public async Task<JsonResult> GetCredentials()
        {  
            var tokenResult = await _viewDataClient.AuthenticateAsync();

            if (!tokenResult.IsOk())
            {
                return Json("", JsonRequestBehavior.AllowGet);
            }

            var bucketResult = await CreateBucket();

            if (!bucketResult.IsOk())
            {
                return Json("", JsonRequestBehavior.AllowGet);
            }

            return Json(tokenResult.AccessToken, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [ActionName("Models")]
        public JsonResult GetModels()
        {
            using (var db = new DropDB())
            {
                var models = db.models.ToList();

                string json = JsonConvert.SerializeObject(models);

                return Json(json, JsonRequestBehavior.AllowGet);
            }
        }

        private void ClearDatabase()
        {
            using (var db = new DropDB())
            {
                foreach (var model in db.models)
                {
                    db.Entry(model).State = System.Data.EntityState.Deleted;
                    db.SaveChanges();
                }
            }
        }

        private async Task<BucketDetailsResponse> CreateBucket()
        {
            var bucketResponse = await _viewDataClient.GetBucketDetailsAsync(BUCKET_NAME);

            if (bucketResponse.IsOk())
            {
                return bucketResponse;
            }
            else if (bucketResponse.Error.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                BucketCreationData bucketData = new BucketCreationData(
                    BUCKET_NAME, BucketPolicyEnum.kPersistent);

                BucketDetailsResponse bucketCreationResponse =
                   await _viewDataClient.CreateBucketAsync(bucketData);

                return bucketCreationResponse;
            }
            else
            {
                return bucketResponse;
            }
        }

        //private StreamContent CreateFileContent(Stream stream, string fileName, string contentType)
        //{
        //    var fileContent = new StreamContent(stream);

        //    fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        //    {
        //        Name = "\"files\"",
        //        FileName = "\"" + fileName + "\""
        //    }; 

        //    fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        //    return fileContent;
        //}

        [HttpPost]
        public async Task UploadFiles(IEnumerable<HttpPostedFileBase> files)
        {
            foreach(var file in files)
            {
                var fi = FileUploadInfo.CreateFromStream(
                    file.FileName,
                    file.InputStream);

                var objDetailsResponse = await _viewDataClient.UploadFileAsync(
                    BUCKET_NAME,
                    fi);

                if(!objDetailsResponse.IsOk())
                {
                    continue;
                }

                var objDetails = objDetailsResponse.Objects[0];

                var bubbleResponse = await _viewDataClient.RegisterAsync(
                    objDetails.FileId);

                if (!bubbleResponse.IsOk())
                {
                    continue;
                }

                using (var db = new DropDB())
                {
                    var entry = new model();

                    entry.ModelId = objDetails.FileId.ToBase64();
                    entry.Name = objDetails.ObjectKey.Split(new char[]{'.'})[0];

                    db.models.Add(entry);
                    db.SaveChanges();

                    string url = "http://" + Request.Url.Authority + "/api/push/message";

                    PushMessage(url, new PushMessage(entry.Name, entry.ModelId));
                } 
            }
        }

        private async void PushMessage(string url, PushMessage msg)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = 
                    await client.PostAsJsonAsync(url, msg);
            }
        }
    }
}
