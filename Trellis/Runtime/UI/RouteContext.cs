using System;
using System.Collections.Generic;

namespace Trellis.UI
{
    /// <summary>
    /// Context passed to panels during route navigation. Contains the route path
    /// and parsed query parameters.
    /// </summary>
    public class RouteContext
    {
        private readonly Dictionary<string, string> parameters;

        /// <summary>
        /// The full route path (e.g., "/settings/audio").
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Number of query parameters.
        /// </summary>
        public int ParameterCount => parameters.Count;

        public RouteContext(string path, Dictionary<string, string> parameters)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            this.parameters = parameters ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets a query parameter value by name. Returns null if not found.
        /// </summary>
        public string GetParam(string name)
        {
            parameters.TryGetValue(name, out string value);
            return value;
        }

        /// <summary>
        /// Gets a query parameter as an integer. Returns defaultValue if not found or not parseable.
        /// </summary>
        public int GetParamInt(string name, int defaultValue = 0)
        {
            if (parameters.TryGetValue(name, out string value) && int.TryParse(value, out int result))
            {
                return result;
            }

            return defaultValue;
        }

        /// <summary>
        /// Returns true if the parameter exists.
        /// </summary>
        public bool HasParam(string name)
        {
            return parameters.ContainsKey(name);
        }

        /// <summary>
        /// Parses a route string into path and parameters.
        /// e.g., "/inventory?itemId=42&tab=weapons" → path="/inventory", params={itemId:42, tab:weapons}
        /// </summary>
        public static RouteContext Parse(string route)
        {
            if (string.IsNullOrEmpty(route))
            {
                return new RouteContext("/", new Dictionary<string, string>());
            }

            int queryIndex = route.IndexOf('?');
            if (queryIndex < 0)
            {
                return new RouteContext(route, new Dictionary<string, string>());
            }

            string path = route.Substring(0, queryIndex);
            string queryString = route.Substring(queryIndex + 1);
            var parameters = new Dictionary<string, string>();

            if (queryString.Length > 0)
            {
                string[] pairs = queryString.Split('&');
                for (int i = 0; i < pairs.Length; i++)
                {
                    string pair = pairs[i];
                    int eqIndex = pair.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        string key = pair.Substring(0, eqIndex);
                        string value = pair.Substring(eqIndex + 1);
                        parameters[key] = value;
                    }
                    else if (pair.Length > 0)
                    {
                        parameters[pair] = string.Empty;
                    }
                }
            }

            return new RouteContext(path, parameters);
        }
    }
}
