using ApiDocs.Validation.Config;
using ApiDocs.Validation.Error;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.Logger
{

    internal interface IOutputDelegate
    {
        Task ReportTestCompleteAsync(TestEngine.DocTest docTest, ValidationError[] messages);
        Task StartTestAsync(TestEngine.DocTest test);
        Task CloseAsync(TestEngine engine);
    }


    abstract class OutputDelegate : IOutputDelegate
    {
        protected ReportingEngineParameters Report { get; private set; }
        protected SeverityParameters Severity { get; private set; }

        protected OutputDelegate(ReportingEngineParameters report, SeverityParameters severity)
        {
            Report = report;
            Severity = severity;
        }

        public abstract Task StartTestAsync(TestEngine.DocTest test);

        public abstract Task ReportTestCompleteAsync(TestEngine.DocTest test, ValidationError[] messages);

        public abstract Task CloseAsync(TestEngine engine);

        protected virtual async Task WriteValidationErrorAsync(System.IO.TextWriter writer, string indent, MessageLevel level, ValidationError message)
        {
            string prefix = (level == MessageLevel.Error) ? "Error: " : (level == MessageLevel.Warning) ? "Warning: " : "";
            await WriteIndentedTextAsync(writer, indent, String.Concat(prefix, message.ErrorText));
        }

        protected virtual async Task WriteIndentedTextAsync(TextWriter writer, string indent, string text)
        {
            using (StringReader reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    await writer.WriteLineAsync(string.Concat(indent, line));
                }
            }
        }
    }
}
