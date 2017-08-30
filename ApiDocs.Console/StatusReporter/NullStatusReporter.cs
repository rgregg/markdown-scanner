using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.ConsoleApp.StatusReporter
{
    class NullStatusReporter : IReportingAgent
    {
        public Task AddMessageAsync(string message, MessageCategory category = MessageCategory.Information, string details = null)
        {
            return Task.FromResult<bool>(false);
        }

        public Task RecordTestAsync(string testName, string testFramework = null, string filename = null, TestOutcome outcome = TestOutcome.None, long durationInMilliseconds = 0, string errorMessage = null, string errorStackTrace = null, string stdOut = null, string stdErr = null)
        {
            return Task.FromResult<bool>(false);
        }
    }
}
