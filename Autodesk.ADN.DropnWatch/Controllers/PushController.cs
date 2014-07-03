using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.IO;
using System.Net.Http.Headers;
using System.Net;
using System.Collections.Concurrent;
using System.Threading;
using Newtonsoft.Json;
using System.Diagnostics;
using DropnWatch.Models;

namespace DropnWatch.Controllers
{
    public class PushController : ApiController
    {
        private static readonly ConcurrentDictionary <string, StreamWriter> _subscribers =
            new ConcurrentDictionary<string, StreamWriter> ();

        public HttpResponseMessage Get(HttpRequestMessage request)
        {
            HttpResponseMessage response = request.CreateResponse();
            response.Content = new PushStreamContent(OnStreamAvailable, "text/event-stream");
            return response;
        }

        [HttpPost]
        [ActionName("PushMessage")]
        public void PushMessage(PushMessage msg)
        {
            msg.Dt = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
            MessageCallback(msg);
        }

        public static void OnStreamAvailable(
            Stream stream,
            HttpContent headers,
            TransportContext context)
        {
            var key = Guid.NewGuid().ToString();

            var sw = new StreamWriter(stream);

            _subscribers.TryAdd(key, sw);
        }

        private static void MessageCallback(PushMessage msg)
        {
            foreach (var subscriber in _subscribers)
            {
                try
                {
                    subscriber.Value.WriteLine("data:" + JsonConvert.SerializeObject(msg) + "\n");
                    subscriber.Value.Flush();
                }
                catch(Exception ex)
                {
                    StreamWriter sw;
                    _subscribers.TryRemove(subscriber.Key, out sw);

                    continue;
                }
            }
        }
    }
}