﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MockHttpServer
{
    public class MockHttpHandler
    {
        private readonly Regex _comparisonRegex;
        private readonly List<string> _urlParameterNames = new List<string>(); //stores the names of any parameters in the url (ex. 'books/{category}/{id}')

        public MockHttpHandler(string url, string httpMethod, Func<HttpListenerRequest, HttpListenerResponse, Dictionary<string, string>, string> handlerFunction)
        {
            Url = url;
            HttpMethod = httpMethod;
            HandlerFunction = handlerFunction;

            _comparisonRegex = CreateComparisonRegex(url);
        }

        public MockHttpHandler(string url, Func<HttpListenerRequest, HttpListenerResponse, Dictionary<string, string>, string> handlerFunction)
            : this(url, null, handlerFunction)
        { }

        public string Url { get; }
        public string HttpMethod { get; }
        public Func<HttpListenerRequest, HttpListenerResponse, Dictionary<string, string>, string> HandlerFunction { get; }

        /// <summary>
        /// Create a regex for matching Url against a raw url that may have parameter definitions, or a query string in it
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private Regex CreateComparisonRegex(string url)
        {
            //make sure all urls start and end with a forward slash for more consistency when comparing to incoming urls
            var regexString = url + (url.EndsWith("/") ? "?" : "/?");
            regexString = (regexString.StartsWith("/") ? "^" : "^/") + regexString;

            //find all parameters in the url, and adjust the regex to capture them in groups
            var regex = new Regex(@"{(.*?)}");
            foreach (Match match in regex.Matches(regexString))
            {
                regexString = regexString.Replace(match.Value, @"(.*?)");
                _urlParameterNames.Add(match.Groups[1].Value);
            }

            regexString += @"([?].*)?$";

            return new Regex(regexString);
        }

        /// <summary>
        /// Returns true if the Url in this handler matches the raw Url passed in
        /// </summary>
        /// <param name="rawUrl">The part of the url after the 'http://host:port/' part of the complete url</param>
        /// <param name="httpMethod">The HTTP method set in the request object</param>
        /// <param name="parameters">If there are parameter definitions in Url, they will be stored here, with
        /// their value from rawUrl.  This will be null when the method returns false.  If the method returns true,
        /// but there are no parameters, then it will be an empty dictionary</param>
        /// <returns></returns>
        public bool MatchesUrl(string rawUrl, string httpMethod, out Dictionary<string, string> parameters)
        {
            var match = _comparisonRegex.Match(rawUrl);
            bool isMethodMatched = HttpMethod == null || HttpMethod == httpMethod;
            parameters = null;
            if ((match.Success) && (isMethodMatched))
            {
                parameters = new Dictionary<string, string>();
                for (int i = 0; i < _urlParameterNames.Count; i++)
                    parameters[_urlParameterNames[i]] = match.Groups[i + 1].Value;
                return true;
            }

            return false;
        }
    }
}
