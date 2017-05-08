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

namespace ApiDocs.Validation.Config
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    public class SetsConfigFile : ConfigFile
    {

        [JsonProperty("sets")]
        public Dictionary<string, DocSetConfiguration> Sets { get; set; }

        [JsonProperty("default-parameters")]
        public ApiDocsParameters DefaultParameters { get; set; }

        public override bool IsValid
        {
            get
            {
                return Sets != null;
            }
        }
    }

    public class DocSetConfiguration
    {
        [JsonIgnore]
        public string Name { get; set; }

        [JsonProperty("path")]
        public string RelativePath { get; set; }

        [JsonProperty("actions")]
        public DocSetActions Actions { get; set; }
    }

    public class ApiDocsParameters
    {
        [JsonProperty("reporting")]
        public ReportingParameters Reporting { get; set; }

        [JsonProperty("severity")]
        public SeverityParameters Severity { get; set; }

        [JsonProperty("shared")]
        public SharedActionParameters SharedActionParameters { get; set; }

        [JsonProperty("check-docs")]
        public CheckDocsActionParameters CheckDocsParameters { get; set; }

        [JsonProperty("check-links")]
        public CheckLinksActionParameters CheckLinksParameters { get; set; }

        [JsonProperty("check-metadata")]
        public CheckMetadataActionParameters CheckMetadataParameters { get; set; }

        [JsonProperty("check-service")]
        public CheckServiceActionParameters CheckServiceParameters { get; set; }

        [JsonProperty("git-path")]
        public string GitExecutablePath { get; set; }

        [JsonProperty("pull-requests")]
        public PullRequestParameters PullRequests { get; set; }

        [JsonProperty("page-parameters")]
        public Dictionary<string, string> PageParameters { get; set; }

    }

    public class ReportingParameters
    {
        [JsonProperty("console")]
        public ReportingEngineParameters Console { get; set; }

        [JsonProperty("appveyor")]
        public ReportingEngineParameters Appveyor { get; set; }

        [JsonProperty("text-file")]
        public ReportingEngineParameters TextFile { get; set; }

        [JsonProperty("http-tracer")]
        public ReportingEngineParameters HttpTracer { get; set; }
    }

    public class SharedActionParameters
    {
        [JsonProperty("method-filter")]
        public string MethodFilter { get; set; }

        [JsonProperty("filename-filter")]
        public string FilenameFilter { get; set; }

        [JsonProperty("run-all-scenarions")]
        public bool RunAllScenarios { get; set; }

        [JsonProperty("relax-string-validation")]
        public bool RelaxStringValidation { get; set; }

    }

    public class ReportingEngineParameters
    {
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("log-level")]
        public LogLevel Level { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class SeverityParameters
    {
        [JsonProperty("errors")]
        public SeverityLevel Errors { get; set; }
        [JsonProperty("warnings")]
        public SeverityLevel Warnings { get; set; }
        [JsonProperty("messages")]
        public SeverityLevel Messages { get; set; }
    }


    public class DocSetActions
    {
        [JsonProperty("check-links")]
        public CheckLinksActionParameters CheckLinks { get; set; }

        [JsonProperty("check-docs")]
        public CheckDocsActionParameters CheckDocs { get; set; }

        [JsonProperty("check-service")]
        public CheckServiceActionParameters CheckService { get; set; }

        [JsonProperty("check-metadata")]
        public CheckMetadataActionParameters CheckServiceMetadata { get; set; }

        //[JsonProperty("publish-docs")]
        //public PublishDocsActionParameters PublishDocs { get; set; }
    }

    public class CheckLinksActionParameters
    {
        [JsonProperty("links-case-sensitive")]
        public bool LinksAreCaseSensitive { get; set; }

    }

    public class CheckDocsActionParameters : SharedActionParameters
    {
        [JsonProperty("check-structure")]
        public bool ValidateStructure { get; set; }

        [JsonProperty("check-methods")]
        public bool ValidateMethods { get; set; }

        [JsonProperty("check-examples")]
        public bool ValidateExamples { get; set; }

        public CheckDocsActionParameters()
        {
            this.ValidateExamples = true;
            this.ValidateMethods = true;
            this.ValidateStructure = true;
        }

    }

    public class CheckServiceActionParameters : SharedActionParameters
    {
        [JsonProperty("accounts")]
        public string[] Accounts { get; set; }

        [JsonProperty("pause-between-requests")]
        public bool PauseBetweenRequests { get; set; }

        [JsonProperty("headers")]
        public string[] AdditionalHeaders { get; set; }

        [JsonProperty("metadata-level")]
        public string MetadataLevel { get; set; }

        [JsonProperty("branch-name")]
        public string BranchName { get; set; }

        [JsonProperty("run-in-parallel")]
        public bool RunTestsInParallel { get; set; }
    }

    public class CheckMetadataActionParameters
    {
        [JsonProperty("schema-urls")]
        public string[] SchemaUrls { get; set; }
    }

    public class PublishDocsActionParameters
    {
        public string OutputRelativePath { get; set; }

    }

    public class PullRequestParameters
    {
        [JsonProperty("pull-request-detector")]
        public string PullRequestDetector { get; set; }
        [JsonProperty("target-branch")]
        public string TargetBranch { get; set; }
    }

    public enum LogLevel
    {
        [EnumMember(Value = "default")]
        Default = 0,
        [EnumMember(Value = "errorsOnly")]
        ErrorsOnly,
        [EnumMember(Value = "verbose")]
        Verbose
    }

    public enum SeverityLevel
    {
        [EnumMember(Value = "default")]
        Default = 0,
        [EnumMember(Value = "critical")]
        Critical,
        [EnumMember(Value = "warn")]
        Warning,
        [EnumMember(Value = "ignore")]
        Ignored
    }
}
