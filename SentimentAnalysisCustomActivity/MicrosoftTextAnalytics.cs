using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using System.ComponentModel;

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Rest;

namespace SentimentAnalysis
{

    public sealed class MicrosoftTextAnalytics : NativeActivity
    {
        // Define an activity input argument of type string
        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> Text { get; set; }

        [Category("Authentication")]
        public InArgument<string> Key { get; set; }

        [Category("Scores")]
        public OutArgument<double> SentimentScore { get; set; }

        private class ApiKeyServiceClientCredentials : ServiceClientCredentials
        {
            private const string SubscriptionKey = ""; //Insert your Text Anaytics subscription key
            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
                return base.ProcessHttpRequestAsync(request, cancellationToken);
            }
        }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override async void Execute(NativeActivityContext context)
        {
            // Obtain the runtime value of the Text input argument
            string text = context.GetValue(this.Text);

            // Create a client.
            ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
            {
                Endpoint = "https://westus2.api.cognitive.microsoft.com/text/analytics/v2.0"
            }; //Replace 'westus' with the correct region for your Text Analytics subscription

            Console.OutputEncoding = System.Text.Encoding.UTF8;


            // Extracting sentiment
            Console.WriteLine("\n\n===== SENTIMENT ANALYSIS ======");

            var sentimentResults = await client.SentimentAsync(
                false,
                new MultiLanguageBatchInput(
                    new List<MultiLanguageInput>
                    {
                        new MultiLanguageInput("en", "1", text),
                    }));


            // Printing sentiment results
            foreach (var document in sentimentResults.Documents)
            {
                Console.WriteLine($"Document ID: {document.Id} , Sentiment Score: {document.Score:0.00}, Sentiment Statistics: {document.Statistics}");
                SentimentScore.Set(context, document.Score);
            }
        }



    }

    
}
