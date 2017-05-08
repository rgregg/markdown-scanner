using ApiDocs.Validation.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApiDocs.Validation.Config;

namespace ApiDocs.Validation.Logger
{
    /// <summary>
    /// Handles routing log output and test scenarios to the approriate destinations based
    /// on configuration parameters
    /// </summary>
    public class TestEngine
    {
        private string DocSetPath { get; set; }
        private IOutputDelegate Output { get; set; }
        public TestResult OverallResult { get; private set; }
        private ApiDocsParameters Parameters { get; set; }
        public int TestsPerformed { get; private set; }
        public int TestsPassed { get; private set; }
        public int TestsFailed { get; private set; }

        public TestEngine(Config.ApiDocsParameters parameters, string docSetPath)
        {
            DocSetPath = docSetPath;
            Parameters = parameters;

            var outputs = CreateOutputs(parameters.Reporting, parameters.Severity);
            Output = new MulticastOutputDelegate(outputs);

            OverallResult = TestResult.Running;

        }

        /// <summary>
        /// Create a set of output delegates based on the parameters
        /// </summary>
        private IEnumerable<IOutputDelegate> CreateOutputs(ReportingParameters reports, SeverityParameters severity)
        {
            severity = severity ?? new SeverityParameters();

            List<IOutputDelegate> outputs = new List<IOutputDelegate>();
            //if (reports?.Appveyor != null)
            //{
            //    outputs.Add(new AppveyorLogger(reports.Appveyor, severity));
            //}
            if (reports?.Console != null)
            {
                outputs.Add(new ConsoleLogger(reports.Console, severity));
            }
            //if (reports?.HttpTracer != null)
            //{
            //    outputs.Add(new HttpTracerLogger(reports.HttpTracer, severity));
            //}
            //if (reports?.TextFile != null)
            //{
            //    outputs.Add(new TextFileLogger(reports.TextFile, severity));
            //}
            return outputs;
        }

        /// <summary>
        /// Start logging information about a test. This frequently includes starting
        /// a timer for the duration of the test as well.
        /// </summary>
        public async Task<DocTest> StartTestAsync(string name)
        {
            TestsPerformed += 1;

            var test = new DocTest(this, name);
            await Output.StartTestAsync(test);
            return test;
        }

        public async Task CompleteAsync()
        {
            await Output.CloseAsync(this);
        }

        /// <summary>
        /// Represnts the output from an individual test in the system
        /// </summary>
        public class DocTest
        {
            public string Name { get; private set; }
            public TimeSpan Duration { get; set; }
            public TestResult Result { get; set; }
            private DateTimeOffset StartDateTime { get; set; }
            private TestEngine Parent { get; set; }
            private List<ValidationError> Messages { get; set; }

            internal DocTest(TestEngine parent, string testName)
            {
                this.Parent = parent;
                this.Name = testName;
                this.StartDateTime = DateTimeOffset.UtcNow;
                this.Messages = new List<Error.ValidationError>();
                this.Duration = TimeSpan.MaxValue;
                this.Result = TestResult.NotStarted;
            }

            public void LogMessage(ValidationError message)
            {
                if (message != null)
                {
                    Messages.Add(message);
                }
            }

            public void LogMessages(IEnumerable<ValidationError> messages)
            {
                if (messages != null)
                {
                    Messages.AddRange(messages);
                }
            }

            public async Task<TestResult> CompleteAsync(ValidationError error, TestResult? overrideResult = null)
            {
                return await CompleteAsync(new ValidationError[] { error }, overrideResult);
            }

            public async Task<TestResult> CompleteAsync(IEnumerable<ValidationError> errors, TestResult? overrideResult = null)
            {
                Duration = DateTimeOffset.UtcNow.Subtract(this.StartDateTime);
                LogMessages(errors);

                Result = DetermineTestResult();
                await Parent.Output.ReportTestCompleteAsync(this, Messages.ToArray());

                if (Result == TestResult.Passed || Result == TestResult.PassedWithWarnings)
                {
                    Parent.TestsPassed += 1;
                }
                else
                {
                    Parent.TestsFailed += 1;
                }

                Parent.OverallResult = (new TestResult[] { Result, Parent.OverallResult }).Min();
                return Result;
            }

            private TestResult DetermineTestResult()
            {
                var severity = Parent.Parameters.Severity;
                var query = from m in Messages
                            select ResultFromMessage(m, Parent.Parameters.Severity);
                if (query.Any())
                {
                    return query.Min();
                }
                else
                {
                    return TestResult.Passed;
                }
            }

            internal static TestResult ResultFromMessage(ValidationError message, SeverityParameters severity)
            {
                if (message.IsError && ( severity.Errors == SeverityLevel.Critical || severity.Errors == SeverityLevel.Default ))
                {
                    return TestResult.Failed;
                }
                else if (message.IsError && severity.Errors == SeverityLevel.Warning)
                {
                    return TestResult.PassedWithWarnings;
                }
                else if (message.IsWarning && severity.Warnings == SeverityLevel.Critical)
                {
                    return TestResult.Failed;
                }
                else if (message.IsWarning && (severity.Warnings == SeverityLevel.Warning || severity.Warnings == SeverityLevel.Default ))
                {
                    return TestResult.PassedWithWarnings;
                }
                else
                {
                    return TestResult.Passed;
                }
            }

            
        }

        /// <summary>
        /// Determine the message level for a message, based on it's own definition and the severity parameters
        /// </summary>
        /// <param name="message"></param>
        /// <param name="severity"></param>
        /// <returns></returns>
        internal static MessageLevel LevelForMessage(ValidationError message, SeverityParameters severity)
        {
            if (message.IsError && (severity.Errors == SeverityLevel.Critical || severity.Errors == SeverityLevel.Default))
            {
                return MessageLevel.Error;
            }
            else if (message.IsError && severity.Errors == SeverityLevel.Warning)
            {
                return MessageLevel.Warning;
            }
            else if (message.IsWarning && severity.Warnings == SeverityLevel.Critical)
            {
                return MessageLevel.Error;
            }
            else if (message.IsWarning && (severity.Warnings == SeverityLevel.Warning || severity.Warnings == SeverityLevel.Default))
            {
                return MessageLevel.Warning;
            }

            return MessageLevel.Message;
        }

    }

    public enum MessageLevel
    {
        Message = 0,
        Warning,
        Error 
    }

    public enum TestResult
    {
        NotStarted = 0,
        NothingToTest = 50,
        Failed = 100,
        PassedWithWarnings = 200,
        Passed = 300,
        Running = 1000
            
    }

}
