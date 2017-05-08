using ApiDocs.Validation.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApiDocs.Validation.Error;
using System.IO;

namespace ApiDocs.Validation.Logger
{
    internal class ConsoleLogger : OutputDelegate
    {
        public ConsoleLogger(ReportingEngineParameters report, SeverityParameters severity) : base(report, severity)
        {

        }
        public override Task StartTestAsync(TestEngine.DocTest test)
        {
            if (Report.Level == LogLevel.Verbose)
            {
                Console.WriteLine($"Starting test: {test.Name}");
            }
            
            return Task.FromResult<bool>(false);
        }

        public override async Task ReportTestCompleteAsync(TestEngine.DocTest test, ValidationError[] messages)
        {
            using (StringWriter writer = new StringWriter())
            {
                bool hasWrittenHeader = false;
                foreach (var message in messages)
                {
                    var level = TestEngine.LevelForMessage(message, Severity);
                    // Skip messages unless the output is verbose
                    if (level == MessageLevel.Message && Report.Level != LogLevel.Verbose)
                    {
                        continue;
                    }
                    // Skip non-errors if the output is ErrorsOnly
                    if (level != MessageLevel.Error && Report.Level == LogLevel.ErrorsOnly)
                    {
                        continue;
                    }

                    if (!hasWrittenHeader)
                    {
                        await writer.WriteLineAsync($"Results for: '{test.Name}'");
                        hasWrittenHeader = true;
                    }

                    await WriteValidationErrorAsync(writer, "  ", level, message);
                }

                if (Report.Level != LogLevel.ErrorsOnly || hasWrittenHeader)
                {
                    await WriteIndentedTextAsync(writer, string.Empty, $"Test '{test.Name}' result: {test.Result} in {test.Duration.TotalSeconds} seconds");
                }
                string output = writer.ToString();
                if (output.Any())
                {
                    Console.WriteLine(writer.ToString());
                }
            }
        }

        public override Task CloseAsync(TestEngine engine)
        {
            double percent = 100 * ((double)engine.TestsPassed / (double)engine.TestsPerformed);
            Console.WriteLine($"Overall result: {engine.OverallResult}. {engine.TestsPassed} tests of {engine.TestsPerformed} passed ({percent:n1}%)");
            return Task.FromResult<bool>(false);
        }
    }
}
