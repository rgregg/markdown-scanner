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

namespace ApiDocs.Validation.Preprocessors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;

    /// <summary>
    /// Handle pre-processing Markdown files to remove front matter from the file before passing it on to be further processed
    /// </summary>
    internal class FrontMatterParser : IMarkdownProcessor
    {
        private const string FrontMatterBoundary = "---";

        /// <summary>
        /// Look for any front matter in the input file, process the front mater, and return the remaining contents of the file.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string TransformContent(string input, DocFile source)
        {
            StringReader reader = new StringReader(input);
            var line = reader.ReadLine();
            if (!line.Equals(FrontMatterBoundary))
            {
                // File doesn't appear to have front matter, so we bail out quickly.
                return input;
            }

            StringBuilder frontMatterSource = new StringBuilder();
            while(true)
            {
                line = reader.ReadLine();
                if (line.Equals(FrontMatterBoundary))
                    break;

                frontMatterSource.AppendLine(line);
            }

            ProcessFrontMatter(frontMatterSource.ToString());

            // Return the remainder of the string
            return reader.ReadToEnd();
        }

        private void ProcessFrontMatter(string source)
        {
            this.Annotation = new PageAnnotation();

            // Front matter is supposed to be YAML. Our support is going to be limited to just the basics, but we could add a real YAML parser here in the future
            StringReader reader = new StringReader(source);
            string line = reader.ReadLine();
            while(line != null)
            {
                string name, value;
                Http.HttpParser.ParseHeader(line, out name, out value);
                switch(name.ToLower())
                {

                }
                line = reader.ReadLine();
            }
        }

        public PageAnnotation Annotation { get; set; }


    }
}
