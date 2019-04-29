using System;
using System.Activities;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Json;
using Newtonsoft.Json;
using System.Threading;
using Google.Cloud.Language.V1;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;

namespace SentimentAnalysis
{

    public sealed class GoogleNLPSentimentAnalysis : NativeActivity
    {
        // Define an activity input argument of type string
        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> Text { get; set; }

        [Category("Output")]
        public OutArgument<double> SentimentScore { get; set; }

        [Category("Output")]
        public OutArgument<double> SentimentMagnitude { get; set; }

        [Category("Response")]
        public OutArgument<HttpResponseMessage> GoogleNLPResponseMessage { get; set; }

        [Category("Authentication")]
        public InArgument<string> File_Path { get; set; }

        [Category("Authentication")]
        public InArgument<string> API_KEY { get; set; }
        

        // private static readonly HttpClient SingleClient = new HttpClient();

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(NativeActivityContext context)
        {
            var text = Text.Get(context);
            string filePath = File_Path.Get(context);
            string apiKey = API_KEY.Get(context);

            if (filePath != null)
            {
                var credential = GoogleCredential.FromFile(filePath).CreateScoped(LanguageServiceClient.DefaultScopes);
                var channel = new Grpc.Core.Channel(LanguageServiceClient.DefaultEndpoint.ToString(), credential.ToChannelCredentials());

                var client = LanguageServiceClient.Create(channel);

                var response = client.AnalyzeSentiment(new Document()
                {
                    Content = text,
                    Type = Document.Types.Type.PlainText
                });

                var sentiment = response.DocumentSentiment;

                // Console.WriteLine($"Score: {sentiment.Score}");
                // Console.WriteLine($"Magnitude: {sentiment.Magnitude}");

                SentimentScore.Set(context, sentiment.Score);
                SentimentMagnitude.Set(context, sentiment.Magnitude);

            }

            else if (apiKey != null) {
                // @TODO Authentication using API Key [Work in progress]
                var nlp_base_url = "https://language.googleapis.com/v1beta2/";
                var operation = "documents:analyzeSentiment";
                var key = "?key=" + API_KEY.Get(context);


                var request_url = nlp_base_url + operation + key;

                object content = new
                {
                    document = new
                    {
                        content = text,
                        type = "PLAIN_TEXT"
                    },
                    encodingType = "UTF8"
                };


                Task<(HttpResponseMessage, String)> response_message = Task.Run(() => PostBasicAsync(request_url, content));
                response_message.Wait();
                (HttpResponseMessage result_json, String stringContent) = response_message.Result;

                // string request_content = stringContent.ReadAsStringAsync().Result;

                // ContentBody.Set(context, "\nStringContent - \n" + stringContent);

                GoogleNLPResponseMessage.Set(context, result_json);
            }
        }


        public static async Task<(HttpResponseMessage, String)> PostBasicAsync(string url, object request_body)
        {
            // HttpResponseMessage response_message;
            
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                var json = JsonConvert.SerializeObject(request_body);
                using (var stringContent = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    using (var response = await client
                        .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                        .ConfigureAwait(false))
                    {
                        // response.EnsureSuccessStatusCode();
                        return (response, stringContent.ReadAsStringAsync().Result);
                    }
                }
            }
        }

    }
}

