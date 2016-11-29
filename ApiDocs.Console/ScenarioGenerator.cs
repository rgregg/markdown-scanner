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

namespace ApiDocs.ConsoleApp
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using ApiDocs.Validation;
    using ApiDocs.Validation.Params;
    using Newtonsoft.Json;
    using ApiDocs.Validation.Http;

    public static class ScenarioGenerator
    {
        /// <summary>
        /// Creates a new scenario file with a given filename in the DocSet working folder that defines a basic scenario for each method in methods.
        /// These scenarios can then be customized to fill in additional values / setup methods.
        /// </summary>
        /// <param name="methods"></param>
        /// <returns></returns>
        internal static async Task Generate(IEnumerable<MethodDefinition> methods, string outputFilename, DocSet docs)
        {
            List<ScenarioDefinition> newScenarios = new List<ScenarioDefinition>();

            foreach (var method in methods)
            {
                if (string.IsNullOrEmpty(method.Identifier))
                    continue;

                ScenarioDefinition def = new ScenarioDefinition();
                def.MethodName = method.Identifier;
                def.Enabled = true;
                def.Description = $"Generated scenario for {method.Identifier}";
                def.RequestParameters = DictionaryForRequestPlaceholders(method);
                def.RequiredScopes = null;
                newScenarios.Add(def);
            }


            if (newScenarios.Any())
            {
                ScenarioFile file = new ScenarioFile();
                file.Scenarios = newScenarios.ToArray();

                var outputPath = System.IO.Path.Combine(docs.SourceFolderPath, outputFilename);
                using (var writer = System.IO.File.CreateText(outputPath))
                {
                    await writer.WriteAsync(JsonConvert.SerializeObject(file, Formatting.Indented));
                    await writer.FlushAsync();
                }
            }
        }

        /// <summary>
        /// Return a dictionary of placeholders found in a method's request.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private static Dictionary<string, string> DictionaryForRequestPlaceholders(MethodDefinition method)
        {
            HttpParser parser = new HttpParser();
            var request = parser.ParseHttpRequest(method.Request);

            Dictionary<string, string> output = new Dictionary<string, string>();

            var matches = System.Text.RegularExpressions.Regex.Matches(request.Url, "{.*?}");
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                if (m.Value != "{}")
                {
                    output.Add(m.Value, "value-literal");
                }
            }

            return output;
        }
    }
}
