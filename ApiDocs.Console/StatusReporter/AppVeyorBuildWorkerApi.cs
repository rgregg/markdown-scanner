/*
 * Markdown Scanner
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the ""Software""), to deal in 
 * the Software without restriction, including without limitation the rights to use, 
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDocs.ConsoleApp.StatusReporter
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class AppVeyorBuildWorkerApi : IReportingAgent
    {
        public Uri UrlEndPoint { get; set; }
        private static readonly JsonSerializerSettings CachedJsonSettings;

        static AppVeyorBuildWorkerApi()
        {
            CachedJsonSettings =  new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore };
            CachedJsonSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
        }

        public AppVeyorBuildWorkerApi(Uri apiUrl)
        {
            this.UrlEndPoint = apiUrl;
        }

        public AppVeyorBuildWorkerApi()
        {
            this.UrlEndPoint = null;
        }

        public async Task AddMessageAsync(string message, MessageCategory category = MessageCategory.Information, string details = null)
        {
            try
            {
                var body = new { message = message, category = category, details = details };
                await this.PostToApiAsync("api/build/messages", body);
            }
            catch
            {
                // ignored
            }
        }


        public async Task RecordTestAsync(string testName, string testFramework = null, string filename = null, TestOutcome outcome = TestOutcome.None, long durationInMilliseconds = 0, string errorMessage = null, string errorStackTrace = null, string stdOut = null, string stdErr = null)
        {
            try
            {
                var body = new
                {
                    testName = testName,
                    testFramework = testFramework,
                    fileName = filename,
                    outcome = outcome,
                    durationMilliseconds = durationInMilliseconds,
                    ErrorMessage = errorMessage,
                    ErrorStackTrace = errorStackTrace,
                    StdOut = stdOut,
                    StdErr = stdErr
                };
                await this.PostToApiAsync("api/tests", body);
            }
            catch
            {
                // ignored
            }
        }

        private async Task PostToApiAsync(string path, object body)
        {
#if DEBUG
            if (this.UrlEndPoint == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    string.Format("WorkerApi: {0}\r\n{1}", path, JsonConvert.SerializeObject(body, CachedJsonSettings)));
            }
#endif
            if (this.UrlEndPoint == null) return;

            var targetUrl = new Uri(this.UrlEndPoint, path);
            var request = WebRequest.CreateHttp(targetUrl);
            request.Method = "POST";
            request.ContentType = "application/json";

            if (null != body)
            {
                using (var writer = new StreamWriter(await request.GetRequestStreamAsync()))
                {
                    var bodyString = JsonConvert.SerializeObject(body, CachedJsonSettings);
                    await writer.WriteAsync(bodyString);
                    await writer.FlushAsync();
                }
            }

            HttpWebResponse httpResponse = null;
            try
            {
                var response = await request.GetResponseAsync();
                httpResponse = response as HttpWebResponse;
            }
            catch (WebException webEx)
            {
                httpResponse = webEx.Response as HttpWebResponse;
            }

            if (null != httpResponse)
            {
                if (httpResponse.StatusCode != HttpStatusCode.OK && httpResponse.StatusCode != HttpStatusCode.NoContent)
                {
                    Console.WriteLine("BuildWorkerApi response was {0}: {1}", (int)httpResponse.StatusCode, httpResponse.StatusDescription);
                }
                httpResponse.Dispose();
            }
        }
    }



}
