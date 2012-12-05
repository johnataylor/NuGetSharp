using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGetSharp
{
    class Program
    {
        static Task<string> DownloadPackage(string host, string port, string path, string package, string userAgent, string nugetOperation, string folder, string filenameMangle = "")
        {
            HttpClient client = new HttpClient();

            string requestUri = "http://" + host + ":" + port + path;

            CancellationTokenSource cts = new CancellationTokenSource();

            //Task<HttpResponseMessage> responseTask = client.GetAsync(requestUri, cts.Token);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            request.Headers.Add("user-agent", userAgent);
            request.Headers.Add("NuGet-Operation", nugetOperation);

            Task<HttpResponseMessage> responseTask = client.SendAsync(request);

            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            responseTask.ContinueWith((rt) =>
            {
                HttpResponseMessage responseMessage = rt.Result;

                if (responseMessage.StatusCode == HttpStatusCode.OK)
                {
                    try
                    {
                        string filename;

                        ContentDispositionHeaderValue contentDisposition = responseMessage.Content.Headers.ContentDisposition;
                        if (contentDisposition != null)
                        {
                            filename = contentDisposition.FileName;
                        }
                        else
                        {
                            filename = package.Replace('/', '_');
                        }

                        FileStream fileStream = File.Create(folder + filename + filenameMangle);

                        Task contentTask = responseMessage.Content.CopyToAsync(fileStream);

                        contentTask.ContinueWith((ct) =>
                        {
                            try
                            {
                                fileStream.Close();
                                tcs.SetResult(filename);
                            }
                            catch (Exception e)
                            {
                                tcs.SetException(e);
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                    }
                }
                else
                {
                    string msg = string.Format("Http StatusCode: {0}", responseMessage.StatusCode);
                    tcs.SetException(new ApplicationException(msg));
                }
            });

            return tcs.Task;
        }

        static void Test0()
        {
            try
            {
                string host = "preview.nuget.org";
                string port = "80";
                string package = "EntityFramework/5.0.0";
                string path = "/api/v2/Package/" + package;

                string userAgent = "NuGetSharp Testing (Test0)";
                string nugetOperation = "Install";

                string folder = "./";

                Task<string> downloadTask = DownloadPackage(host, port, path, package, userAgent, nugetOperation, folder);

                // note Task.Result will complete synchronously if we just don't call it from the Continuation
                string filename = downloadTask.Result;

                Console.WriteLine("downloaded: {0}", filename);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void Test1()
        {
            try
            {
                string host = "preview.nuget.org";
                string port = "80";
                string package = "EntityFramework/5.0.0";
                string path = "/api/v2/Package/" + package;

                string userAgent = "NuGetSharp Testing (Test1)";
                string nugetOperation = "Install";

                string folder = "./";

                const int Parallel = 10;
                const int Sequential = 3;

                int index = 0;

                for (int j = 0; j < Sequential; j++)
                {
                    Task<string>[] downloadTasks = new Task<string>[Parallel];

                    for (int i = 0; i < Parallel; i++)
                    {
                        string filenameMangle = "_" + (index++).ToString();

                        downloadTasks[i] = DownloadPackage(host, port, path, package, userAgent, nugetOperation, folder, filenameMangle);
                    }

                    Task allDone = Task.WhenAll(downloadTasks);

                    allDone.Wait();
                }

                Console.WriteLine("all done");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        static void Main(string[] args)
        {
            try
            {
                //Test0();
                Test1();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
