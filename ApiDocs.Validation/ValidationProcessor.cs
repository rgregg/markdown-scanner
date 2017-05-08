using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApiDocs.Validation.Config;
using ApiDocs.Validation.Error;
using ApiDocs.Validation.Logger;

namespace ApiDocs.Validation
{
    /// <summary>
    /// Wraps the various validation methods implemented into configuration + simple functions
    /// </summary>
    public static class ValidationProcessor
    {
        /// <summary>
        /// Performs the check-links operation, which validates that there are no broken links within the DocSet.
        /// </summary>
        public static  async Task<TestResult> CheckLinksAsync(ApiDocsParameters parameters, DocSet docs, TestEngine tester = null)
        {
            if (null == parameters) throw new ArgumentNullException("parameters");
            if (null == docs) throw new ArgumentNullException("docs");

            var filesOfInterest = ComputeFilesOfInterest(parameters, docs);
            tester = tester ?? new TestEngine(parameters, docs.SourceFolderPath);

            await ValidateLinks(parameters.CheckLinksParameters, docs, filesOfInterest, tester);

            return tester.OverallResult;
        }

        /// <summary>
        /// Perform a series of tests on the DocSet provided based on the parameters specified.
        /// These tests will evaluate the content of the documentation files to ensure correctness.
        /// </summary>
        public static async Task<TestResult> CheckDocsAsync(ApiDocsParameters parameters, DocSet docs, TestEngine tester = null)
        {
            if (null == parameters) throw new ArgumentNullException("parameters");
            if (null == docs) throw new ArgumentNullException("docs");

            var checkDocs = parameters.CheckDocsParameters ?? new CheckDocsActionParameters();

            var filesOfInterest = ComputeFilesOfInterest(parameters, docs);
            tester = tester ?? new TestEngine(parameters, docs.SourceFolderPath);

            // Look for structure errors in the docs
            if (checkDocs.ValidateStructure)
            {
                await ValidateDocumentStructure(checkDocs, docs, filesOfInterest, tester);
            }

            // Check for errors in request/response pairs (methods)
            if (checkDocs.ValidateMethods)
            {
                await CheckMethodsAsync(checkDocs, docs, filesOfInterest, tester);
            }

            // Check for errors in code examples
            if (checkDocs.ValidateExamples)
            {
                await CheckExamplesAsync(checkDocs, docs, filesOfInterest, tester);
            }

            return tester.OverallResult;
        }

        /// <summary>
        /// Validate that links within the documentation are correct.
        /// </summary>
        private static async Task ValidateLinks(CheckLinksActionParameters parameters, DocSet docs, IEnumerable<DocFile> filesOfInterest, TestEngine tester)
        {
            parameters = parameters ?? new CheckLinksActionParameters();

            var test = await tester.StartTestAsync("Validate links");
            IEnumerable<DocFile> files = (filesOfInterest.Any()) ? filesOfInterest : docs.Files;

            List<ValidationError> detectedErrors = new List<ValidationError>();
            foreach(var file in files)
            {
                ValidationError[] errors;
                file.ValidateNoBrokenLinks(true, out errors, parameters.LinksAreCaseSensitive);
                detectedErrors.AddRange(errors);
            }
            await test.CompleteAsync(detectedErrors);
        }

        /// <summary>
        /// Validate that the structure of the document is correct. This includes headings, table columns, and other structural elements
        /// </summary>
        private static async Task ValidateDocumentStructure(CheckDocsActionParameters parameters, DocSet docs, IEnumerable<DocFile> filesOfInterest, TestEngine tester)
        {
            var test = await tester.StartTestAsync("Validate document structure");
            IEnumerable<DocFile> files = (filesOfInterest.Any()) ? filesOfInterest : docs.Files;

            var detectedErrors = new List<ValidationError>();
            foreach (var file in files)
            {
                var errors = file.CheckDocumentStructure();
                detectedErrors.AddRange(errors);
            }

            await test.CompleteAsync(detectedErrors);
        }

        /// <summary>
        /// Validate that code examples within the documentation are correct.
        /// </summary>
        private static async Task CheckExamplesAsync(CheckDocsActionParameters parameters, DocSet docs, IEnumerable<DocFile> filesOfInterest, TestEngine tester)
        {
            IEnumerable<DocFile> files = (filesOfInterest.Any()) ? filesOfInterest : docs.Files;
            foreach (var file in files)
            {
                if (!file.Examples.Any())
                {
                    continue;
                }

                foreach (var example in file.Examples)
                {
                    if (example.Metadata == null)
                    {
                        continue;
                    }

                    var test = await tester.StartTestAsync($"Example: {example.Metadata.MethodName} in {file.DisplayName}");
                    ValidationError[] errors;
                    switch (example.Language)
                    {
                        case CodeLanguage.Json:
                            {
                                docs.ResourceCollection.ValidateJsonExample(example.Metadata, example.SourceExample, out errors, new Json.ValidationOptions { RelaxedStringValidation = parameters.RelaxStringValidation });
                                break;
                            }
                        default:
                            {
                                errors = new ValidationError[] { new ValidationWarning(ValidationErrorCode.UnsupportedLanguage, file.DisplayName, $"Example {example.Metadata.MethodName} was skipped because {example.Language} is not supported.") };
                                break;
                            }
                    }
                    await test.CompleteAsync(errors);
                }
            }
        }

        /// <summary>
        /// Validate that request/response pairs (methods) within the documentation are correct.
        /// </summary>
        private static async Task CheckMethodsAsync(CheckDocsActionParameters parameters, DocSet docs, IEnumerable<DocFile> filesOfInterest, TestEngine tester)
        {
            IEnumerable<DocFile> files = (filesOfInterest.Any()) ? filesOfInterest : docs.Files;
            var methods = GetMethodsForTesting(files, parameters);

            if (!methods.Any())
            {
                var test = await tester.StartTestAsync("check-methods");
                await test.CompleteAsync(new ValidationError(ValidationErrorCode.NoMatchingMethods, null, $"No methods matches the provided filters."), TestResult.Failed);
                return;
            }

            foreach(var method in methods)
            {
                var test = await tester.StartTestAsync($"check-method: {method.Identifier} in {method.SourceFile.DisplayName}");

                // Make sure the method has a response
                if (string.IsNullOrEmpty(method.ExpectedResponse))
                {
                    await test.CompleteAsync(new ValidationError(ValidationErrorCode.RequestWasEmptyOrNull, method.SourceFile.DisplayName, $"Response was null or empty."), TestResult.Failed);
                    continue;
                }

                var parser = Http.HttpParser.Default;
                ValidationError[] errors;
                try
                {
                    var expectedResponse = parser.ParseHttpResponse(method.ExpectedResponse);
                    method.ValidateResponse(expectedResponse, null, null, out errors, new Json.ValidationOptions { RelaxedStringValidation = parameters.RelaxStringValidation });
                }
                catch (Exception ex)
                {
                    errors = new ValidationError[] { new ValidationError(ValidationErrorCode.ExceptionWhileValidatingMethod, method.SourceFile.DisplayName, ex.Message) };
                }

                await test.CompleteAsync(errors);
            }
        }

        /// <summary>
        /// Returns an iterator for MethodDefintion instances in a collection of DocFiles that match a specified set of filters.
        /// </summary>
        private static IEnumerable<MethodDefinition> GetMethodsForTesting(IEnumerable<DocFile> files, CheckDocsActionParameters options)
        {
            if (!string.IsNullOrEmpty(options.MethodFilter))
            {
                return (from m in GetMethodsInFiles(files)
                        where m.Identifier.IsWildcardMatch(options.MethodFilter)
                        select m);
            }
            else if (!string.IsNullOrEmpty(options.FilenameFilter))
            {
                var matchingFiles = (from f in files where f.DisplayName.IsWildcardMatch(options.FilenameFilter) select f);
                return GetMethodsInFiles(matchingFiles);
            }
            else
            {
                return GetMethodsInFiles(files);                
            }
        }

        /// <summary>
        /// Iterate through the methods in a collection of files
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private static IEnumerable<MethodDefinition> GetMethodsInFiles(IEnumerable<DocFile> files)
        {
            foreach(var file in files)
            {
                foreach(var method in file.Requests)
                {
                    yield return method;
                }
            }
        }

        /// <summary>
        /// Uses the parameters and docs to figure out a set of interesting files within the docs.
        /// </summary>
        private static IEnumerable<DocFile> ComputeFilesOfInterest(ApiDocsParameters parameters, DocSet docs)
        {
            // If this is a pull request, and we have a target branch to compare with, then find the files that are different
            if (!string.IsNullOrEmpty(parameters.PullRequests?.PullRequestDetector) && !string.IsNullOrEmpty(parameters.PullRequests?.TargetBranch))
            {
                GitHelper helper = new GitHelper(parameters.GitExecutablePath, docs.SourceFolderPath);
                var pathsOfInterestingFiles = helper.FilesChangedFromBranch(parameters.PullRequests.TargetBranch);

                foreach(var path in pathsOfInterestingFiles)
                {
                    var file = docs.LookupFileForPath(path);
                    if (null != file)
                    {
                        yield return file;
                    }
                }
            }
            yield break;
        }
        
    }
}
