using Levridge.AuthenticationUtility;
using Levridge.ODataDataSources.DynamicsCRM;
using Microsoft.Extensions.Configuration;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CDSDataImportPOC
{

    public class ODataClientServiceFactory
    {
    }

    public interface IODataClientService
    {
        public Task<T> ExecuteBoundActionAsync<T>(T boundEntity, System.Object boundEntityId, String actionName) where T : class;
        public Task ExecuteUnboundActionAsync(Dictionary<String, System.Object> parameters, String actionName);
    }

    public class ODataClientService : IODataClientService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private OAuthHeaderDelegate _headerDelegate;

        private HttpRequestMessage _requestMessage;
        private HttpResponseMessage _responseMessage;
        private String _responseContent;

        public ODataClientService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _headerDelegate = new OAuthHeaderDelegate(ODataClientService.CreateClientConfiguration(configuration));
        }

        public async Task<T> ExecuteBoundActionAsync<T>(T boundEntity, System.Object boundEntityId, String actionName) where T : class
        {
            var client = this.GetODataClient();
            var importClient = client.For<T>()
                .Key(boundEntityId)
                .Action(actionName);
            var actionResult = await importClient.ExecuteAsSingleAsync();

            // Get asyncoperation
            if (String.IsNullOrEmpty(this._responseContent) == false)
            {
                asyncoperation operation = JsonSerializer.Deserialize<asyncoperation>(_responseContent);
                CancellationTokenSource cts = new CancellationTokenSource();
                await this.WaitForAsyncAction(client, operation, cts.Token);
            }
            return actionResult;
        }

        public async Task ExecuteUnboundActionAsync(Dictionary<String, System.Object> parameters, String actionName)
        {
            var client = this.GetODataClient();
            await client
                .Unbound()
                .Action(actionName)
                .Set(parameters)
                .ExecuteAsync();

            // Get asyncoperation
            if (String.IsNullOrEmpty(this._responseContent) == false)
            {
                asyncoperation operation = JsonSerializer.Deserialize<asyncoperation>(_responseContent);
                CancellationTokenSource cts = new CancellationTokenSource();
                await this.WaitForAsyncAction(client, operation, cts.Token);
            }
        }

        private ODataClient GetODataClient()
        {
            var odataConfiguration = new ODataClientConfiguration(_configuration);
            String connectionString = odataConfiguration.ODataEntityPath;

            // Program.Configuration.GetSection("DynamicsCRM").GetConnectionString("ODataEntityPath");
            var httpClient = _httpClientFactory.CreateClient();
            Uri uri = new Uri(connectionString, UriKind.Absolute);
            httpClient.BaseAddress = uri;

            var clientSettings = new ODataClientSettings(httpClient)
            {
                IncludeAnnotationsInResults = true,
                BeforeRequest = this.BeforeRequest,
                AfterResponseAsync = this.AfterResponseAsync,
                IgnoreUnmappedProperties = true,
            };
            
            return new ODataClient(clientSettings);
        }

            private void BeforeRequest(HttpRequestMessage request)
        {
            this._requestMessage = request ?? throw new ArgumentNullException(nameof(request));
            
            // shouldn't be her if there isn't a header
            if(null == this._headerDelegate)
            {
                throw new ApplicationException("Header Delegate has not been created.");
            }

            _headerDelegate.AddRequestAuthenticationHeader(request);

            // Add preference for annotations
            if (request.Headers.TryGetValues("Prefer", out IEnumerable<String> headerValues) == true)
            {
                // If Simple.OData is asking for annoations (Prefer: return= representation)
                // then use the CRM odata.include-annotations method for getting annotations
                if ((headerValues.Contains("return=representation") == true)
                    && (headerValues.Contains("odata.include-annotations=\"*\"") == false))
                {
                    request.Headers.Add("Prefer", "odata.include-annotations=\"*\"");
                }
            }

            // Add Hack for non-support for quoted key values
            String path = System.Web.HttpUtility.UrlDecode(request.RequestUri.AbsolutePath)
                .Replace("('", "(")
                .Replace("')", ")");
            var parsedQuery = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
            var uriBuilder = new UriBuilder(request.RequestUri)
            {
                Path = path,
                Query = parsedQuery.ToString()
            };
            request.RequestUri = uriBuilder.Uri;

            Console.WriteLine($"request being sent to:\n{request.RequestUri.ToString()}:");
            Console.WriteLine($"request content being sent:\n{request.Content}:");
        }

        private async Task AfterResponseAsync(HttpResponseMessage response)
        {
            this._responseMessage = response ?? throw new ArgumentNullException(nameof(response));

            // TODO: Are there scenarios where we need to handle different types
            if ((null != response?.Content))
            {
                _responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"response recieved. Status: {response?.StatusCode}:");
                Console.WriteLine($"response content recieved:\n{_responseContent}:");
            }
        }

        private static ClientConfiguration CreateClientConfiguration(IConfiguration configuration)
        {
            var odataConfiguration = new ODataClientConfiguration(configuration);
            return new ClientConfiguration()
            {
                UriString = odataConfiguration.UriString,
                ActiveDirectoryResource = odataConfiguration.ActiveDirectoryResource,
                ActiveDirectoryTenant = odataConfiguration.ActiveDirectoryTenant,
                ActiveDirectoryClientAppId = odataConfiguration.ActiveDirectoryClientAppId,
                ActiveDirectoryClientAppSecret = odataConfiguration.ActiveDirectoryClientAppSecret,
                ODataEntityPath = odataConfiguration.ODataEntityPath
            };
        }

        private async Task WaitForAsyncAction(ODataClient client, asyncoperation asyncJob, CancellationToken cancellationToken)
        {
            int retryCount = 0;

            while (asyncJob.statecode.Value != (Int32)AsyncOperationState.Completed && 
                cancellationToken.IsCancellationRequested == false && 
                retryCount < 100)
            {
                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                asyncJob = await client.For<asyncoperation>()
                .Key(asyncJob.asyncoperationid)
                .FindEntryAsync();
                Console.WriteLine($"Async operation state is { ((AsyncOperationState)asyncJob.statecode.Value).ToString()}");
            }
            Console.WriteLine($"Async job is { ((AsyncOperationState)asyncJob.statecode.Value).ToString()} with status { ((AsyncOperationStatusCode)asyncJob.statuscode).ToString()}");
        }


    }
}
