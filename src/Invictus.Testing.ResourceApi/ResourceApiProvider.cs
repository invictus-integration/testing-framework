using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Codit.Testing.ResourceApi
{    /// <summary>
     /// Component to provide access in a reliable manner on api testable resources running in Azure.
     /// </summary>
    public class ResourceApiProvider
    {
        private readonly ILogger _logger;
        private DateTimeOffset _startTime = DateTimeOffset.UtcNow;
        private readonly TimeSpan _retryInterval = TimeSpan.FromSeconds(1);
        private bool _hasBodyFilter;
        private bool _hasUrlFilter;
        private bool _hasHeaders;
        private bool _hasActionValues;
        private bool _hasActionParameterValues;
        private bool _skipBearerToken = false;
        private readonly string _definitionName;

        private readonly ResourceApiAuthentication _authentication;

        private string _baseUrlPattern, _urlAction, _urlFilter, _bodyFilter;
        private TimeSpan _timeout = TimeSpan.FromSeconds(90);

        private IDictionary<string, string> _baseUrlValues = new Dictionary<string, string>();
        private IDictionary<string, string> _urlFilterValues = new Dictionary<string, string>();
        private IDictionary<string, string> _bodyFilterValues = new Dictionary<string, string>();
        private IDictionary<string, string> _headerValues = new Dictionary<string, string>();
        private IDictionary<string, string> _actionValues = new Dictionary<string, string>();
        private IDictionary<string, string> _actionParameterValues = new Dictionary<string, string>();

        //private static System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient();

        private ResourceApiProvider(
            string definitionName,
            ResourceApiAuthentication authentication,
            ILogger logger)
        {
            Guard.NotNull(definitionName, nameof(definitionName));
            Guard.NotNull(authentication, nameof(authentication));
            Guard.NotNull(logger, nameof(logger));

            _definitionName = definitionName;
            _authentication = authentication;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ResourceApiProvider"/> class.
        /// </summary>
        /// <param name="definitionName">The context for the Api connection with Azure.</param>
        /// <param name="authentication">The authentication mechanism to authenticate with Azure.</param>
        public static ResourceApiProvider LocatedAt(
            string definitionName,
            ResourceApiAuthentication authentication)
        {
            Guard.NotNull(definitionName, nameof(definitionName));
            Guard.NotNull(authentication, nameof(authentication));

            return LocatedAt(definitionName, authentication, NullLogger.Instance);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ResourceApiProvider"/> class.
        /// </summary>
        /// <param name="definitionName">The context for the Api connection with Azure.</param>
        /// <param name="authentication">The authentication mechanism to authenticate with Azure.</param>
        /// <param name="logger">The instance to write diagnostic trace messages while interacting with the provider.</param>
        public static ResourceApiProvider LocatedAt(
            string definitionName,
            ResourceApiAuthentication authentication,
            ILogger logger)
        {
            Guard.NotNull(definitionName, nameof(definitionName));
            Guard.NotNull(authentication, nameof(authentication));

            logger = logger ?? NullLogger.Instance;
            return new ResourceApiProvider(definitionName, authentication, logger);
        }

        /// <summary>
        /// Sets the values to build the Resource API Url.
        /// </summary>
        /// <param name="baseUrlValues">the dictionary of values to build the Resource API Url.</param>
        public ResourceApiProvider WithBaseUrlValues(Dictionary<string, string> baseUrlValues)
        {
            Guard.NotNull(baseUrlValues, nameof(baseUrlValues));

            _baseUrlValues = baseUrlValues;
            return this;
        }

        /// <summary>
        /// Sets the values to build the Url Filter.
        /// </summary>
        /// <param name="urlFilterValues">the dictionary of values to build the Resource API Url Filter.</param>
        public ResourceApiProvider WithUrlFilterValues(Dictionary<string, string> urlFilterValues)
        {
            _urlFilterValues = urlFilterValues;
            return this;
        }

        /// <summary>
        /// Sets the values to build the Body Filter.
        /// </summary>
        /// <param name="bodyFilterValues">the dictionary of values to build the Resource API Body Filter.</param>
        public ResourceApiProvider WithBodyFilterValues(Dictionary<string, string> bodyFilterValues)
        {
            Guard.NotNull(bodyFilterValues, nameof(bodyFilterValues));

            _bodyFilterValues = bodyFilterValues;
            return this;
        }

        /// <summary>
        /// Sets the values to build the API Header.
        /// </summary>
        /// <param name="headerValues">the dictionary of values to build the Resource API Url.</param>
        public ResourceApiProvider WithHeaderValues(Dictionary<string, string> headerValues)
        {
            Guard.NotNull(headerValues, nameof(headerValues));

            _headerValues = headerValues;
            _hasHeaders = true;
            return this;
        }

        /// <summary>
        /// Sets the values for URL with Action value replacement tags.
        /// </summary>
        /// <param name="actionValues">the tag replacement to build the Resource API Url.</param>
        public ResourceApiProvider WithUrlActionValues(Dictionary<string, string> actionValues)
        {
            Guard.NotNull(actionValues, nameof(actionValues));

            _actionValues = actionValues;
            _hasActionValues = true;
            return this;
        }

        /// <summary>
        /// Sets the values for build a composite URL query.
        /// </summary>
        /// <param name="actionParameterValues">the query elements.</param>
        public ResourceApiProvider WithUrlActionQueryValues(Dictionary<string, string> actionParameterValues)
        {
            Guard.NotNull(actionParameterValues, nameof(actionParameterValues));

            _actionParameterValues = actionParameterValues;
            _hasActionParameterValues = true;
            return this;
        }

        /// <summary>
        /// Sets the time period in which the retrieval of the logic app runs should succeed.
        /// </summary>
        /// <param name="timeout">The period to retrieve logic app runs.</param>
        public ResourceApiProvider WithTimeout(TimeSpan timeout)
        {
            _timeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the Authentication to skip Bearer Token.
        /// </summary>
        /// <param name="skipBearerToken">The flag to skip Bearer Token Auth.</param>
        public ResourceApiProvider WithNoBearerTokenAuthentication(bool skipBearerToken)
        {
            _skipBearerToken = skipBearerToken;
            return this;
        }

        /// <summary>
        /// Sets URL replacement string
        /// </summary>
        /// <param name="baseUrlPattern">The URL replacement string.</param>
        public ResourceApiProvider WithBaseUrlPattern(string baseUrlPattern)
        {
            Guard.NotNullOrEmpty(baseUrlPattern, nameof(baseUrlPattern));
            _baseUrlPattern = baseUrlPattern;
            return this;
        }

        /// <summary>
        /// Sets the urlAction string.
        /// </summary>
        /// <param name="urlAction">The urlAction string.</param>
        public ResourceApiProvider WithUrlAction(string urlAction)
        {
            _urlAction = urlAction;
            return this;
        }

        /// <summary>
        /// Sets the urlFilter string.
        /// </summary>
        /// <param name="urlFilter">The urlFilter string.</param>
        public ResourceApiProvider WithUrlFilter(string urlFilter)
        {
            Guard.NotNullOrEmpty(urlFilter, nameof(urlFilter));
            _urlFilter = urlFilter;
            _hasUrlFilter = true;
            return this;
        }

        /// <summary>
        /// Sets the bodyFilter Pattern.
        /// </summary>
        /// <param name="bodyFilter">The bodyFilter Pattern.</param>
        public ResourceApiProvider WithBodyFilter(string bodyFilter)
        {
            Guard.NotNull(bodyFilter, nameof(bodyFilter));

            _bodyFilter = bodyFilter;
            _hasBodyFilter = true;
            return this;
        }

        /// <summary>
        /// Runs the current url request.
        /// </summary>
        public async Task<string> RunAsync()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(_timeout);

            string requestResult = string.Empty;

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    requestResult = await PostRequestAsync();
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, _definitionName + "=Polling for Resource Api request was faulted: {Message}", exception.Message);
                }
            }

            return requestResult;
        }

        /// <summary>
        /// Starts polling for a series of Resource Api response objects corresponding to the previously set filtering criteria.
        /// </summary>
        public async Task<IEnumerable<object>> PollForResponsesAsync()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(_timeout);

            IEnumerable<object> responses = Enumerable.Empty<object>();

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    responses = await GetResponsesAsync();
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, _definitionName + "=Polling for Resource Api responses was faulted: {Message}", exception.Message);
                }
            }

            return responses ?? Enumerable.Empty<object>();
        }

        /// <summary>
        /// Starts polling for a <paramref name="minimumNumberOfItems"/> corresponding to the previously set filtering criteria.
        /// </summary>
        /// <param name="minimumNumberOfItems">The minimum amount of responses to retrieve.</param>
        public async Task<IEnumerable<object>> PollForResponsesAsync(int minimumNumberOfItems)
        {
            Guard.NotLessThanOrEqualTo(minimumNumberOfItems, 0, nameof(minimumNumberOfItems));

            string amount = minimumNumberOfItems == 1 ? "any" : minimumNumberOfItems.ToString();

            RetryPolicy<IEnumerable<object>> retryPolicy =
                Policy.HandleResult<IEnumerable<object>>(currentResponses =>
                {
                    int count = currentResponses.Count();
                    bool isStillPending = count < minimumNumberOfItems;

                    _logger.LogTrace(_definitionName + "=Polling for {Amount} Resource Api responses, whilst got now {Current} ", amount, count);
                    return isStillPending;
                }).Or<Exception>(ex =>
                {
                    _logger.LogError(ex, _definitionName + "=Polling for Resource Api responses was faulted: {Message}", ex.Message);
                    return true;
                })
                  .WaitAndRetryForeverAsync(index =>
                  {
                      _logger.LogTrace(_definitionName + "=Could not retrieve Resource Api responses in time, wait 1s and try again...");
                      return _retryInterval;
                  });

            PolicyResult<IEnumerable<object>> result =
                await Policy.TimeoutAsync(_timeout)
                            .WrapAsync(retryPolicy)
                            .ExecuteAndCaptureAsync(GetResponsesAsync);

            if (result.Outcome == OutcomeType.Failure)
            {
                if (result.FinalException is null
                    || result.FinalException.GetType() == typeof(TimeoutRejectedException))
                {
                    string filterType = _hasBodyFilter
                        ? $"{Environment.NewLine} with body filter"
                        : $"{Environment.NewLine} with url filter";

                    throw new TimeoutException(
                        $"Could not in the given timeout span ({_timeout:g}) retrieve {amount} Resource Api responses "
                        + $"{Environment.NewLine} with StartTime >= {_startTime.UtcDateTime:O}"
                        + filterType);
                }

                throw result.FinalException;
            }

            _logger.LogTrace(_definitionName + "=Polling finished successful with {Count} Resource Api responses", result.Result.Count());
            return result.Result;
        }

        /// <summary>
        /// Start polling for a single Resource Api response object.
        /// </summary>
        public async Task<object> PollForSingleResponseAsync()
        {
            IEnumerable<object> responses = await PollForResponsesAsync(minimumNumberOfItems: 1);
            return responses.FirstOrDefault();
        }

        private async Task<string> PostRequestAsync()
        {
            string urlFilter = string.Empty;
            StringContent httpFilterBody = null;

            string token = await _authentication.AuthenticateAsync();

            Guard.NotNullOrEmpty(_baseUrlPattern, nameof(_baseUrlPattern));
            Guard.NotNullOrEmpty(_urlAction, nameof(_urlAction));
            Guard.NotNull(_baseUrlValues, nameof(_baseUrlValues));
            var url = BuildPattern(_baseUrlPattern, _baseUrlValues);
            url += _urlAction;

            if (_hasBodyFilter)
            {
                Guard.NotNull(_bodyFilterValues, nameof(_bodyFilterValues));
                    httpFilterBody = new StringContent(BuildPattern(_bodyFilter, _bodyFilterValues),
                    Encoding.UTF8,
                    "application/json");
            }
            else
            {
                if (_hasUrlFilter)
                {
                    Guard.NotNull(_urlFilterValues, nameof(_urlFilterValues));
                    urlFilter = BuildPattern(_urlFilter, _urlFilterValues);
                    url += urlFilter;
                }
            }

            //_headerValues

            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", ("Bearer " + token));

            using (HttpResponseMessage response = await httpClient.PostAsync(url, httpFilterBody))
            {
                return await response.Content.ReadAsStringAsync();
            }
        }

        private async Task<IEnumerable<object>> GetResponsesAsync()
        {
            string urlFilter = string.Empty;
            StringContent httpFilterBody = null;

            string token = await _authentication.AuthenticateAsync();

            Guard.NotNullOrEmpty(_baseUrlPattern, nameof(_baseUrlPattern));
            Guard.NotNullOrEmpty(_urlAction, nameof(_urlAction));
            Guard.NotNull(_baseUrlValues, nameof(_baseUrlValues));
            var url = BuildPattern(_baseUrlPattern, _baseUrlValues);
            url += _urlAction;

            if (_hasBodyFilter)
            {
                Guard.NotNull(_bodyFilterValues, nameof(_bodyFilterValues));
                httpFilterBody = new StringContent(BuildPattern(_bodyFilter, _bodyFilterValues),
                Encoding.UTF8,
                "application/json");
            }
            else
            {
                if (_hasUrlFilter)
                {
                    Guard.NotNull(_urlFilterValues, nameof(_urlFilterValues));
                    urlFilter = BuildPattern(_urlFilter, _urlFilterValues);
                    url += urlFilter;
                }
            }

            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
            if (!_skipBearerToken)
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", ("Bearer " + token));
            }

            if (_hasHeaders)
            {
                foreach (var header in _headerValues)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            if(_hasActionValues)
            {
                foreach (var value in _actionValues)
                {
                    url = url.Replace(value.Key, value.Value);
                }
            }

            if (_hasActionParameterValues)
            {
                string parameterString = string.Empty;
                foreach (var keyPair in _actionParameterValues)
                {
                    parameterString += ((parameterString == string.Empty) ? "" : "&")
                        + string.Format(
                                "{0}={1}",
                                    HttpUtility.UrlEncode(keyPair.Key),
                                     HttpUtility.UrlEncode(keyPair.Value));
                };
                //
                url += "?" + parameterString;
            }

            HttpResponseMessage response = await httpClient.PostAsync(url, httpFilterBody);

            var responseContent = await response.Content.ReadAsStringAsync();

            JObject valueResponses = JObject.Parse(responseContent);

            _logger.LogTrace(_definitionName + "=Query returned {ValueResponsesCount} values", valueResponses.Count);

            var returnResponses = new Collection<object>();
            foreach (var valueResponse in valueResponses)
            {
                returnResponses.Add(valueResponse);
            }

            _logger.LogTrace(_definitionName + "=Query resulted in {ResponseCount} Resource Api responses", returnResponses.Count);
            return returnResponses.AsEnumerable();
        }

        private string BuildPattern(string pattern, IDictionary<string, string> values)
        {
            foreach (var value in values)
            {
                pattern = pattern.Replace(value.Key, value.Value);
            }
            return pattern;
        }

    }
}
