﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script.DependencyInjection
{
    internal class ExternalConfigurationStartupValidator
    {
        private readonly IConfiguration _config;
        private readonly IFunctionMetadataManager _metadataManager;

        public ExternalConfigurationStartupValidator(IConfiguration config, IFunctionMetadataManager metadataManager)
        {
            _config = config;
            _metadataManager = metadataManager;
        }

        /// <summary>
        /// Validates the current configuration against the original configuration. If any values for a trigger
        /// do not match, they are returned via the return value.
        /// </summary>
        /// <param name="originalConfig">The original configuration</param>
        /// <returns>A dictionary mapping function name to a list of the invalid values for that function.</returns>
        public IDictionary<string, IEnumerable<string>> Validate(IConfigurationRoot originalConfig)
        {
            IDictionary<string, IEnumerable<string>> invalidValues = new Dictionary<string, IEnumerable<string>>();

            var functions = _metadataManager.GetFunctionMetadata();

            foreach (var function in functions)
            {
                var trigger = function.Bindings.SingleOrDefault(b => b.IsTrigger);

                IList<string> invalidValuesForFunction = new List<string>();

                // make sure none of the resolved values have changed for the trigger.
                foreach (KeyValuePair<string, JToken> property in trigger.Raw)
                {
                    string lookup = property.Value?.ToString();

                    if (lookup != null)
                    {
                        string originalKey = originalConfig[property.Value?.ToString()];
                        string key = _config[property.Value?.ToString()];
                        if (originalKey != key)
                        {
                            invalidValuesForFunction.Add(lookup);
                        }
                    }
                }

                if (invalidValuesForFunction.Any())
                {
                    invalidValues[function.Name] = invalidValuesForFunction;
                }
            }

            return invalidValues;
        }
    }
}
