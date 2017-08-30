using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.ConsoleApp.StatusReporter
{
    internal interface IReportingAgent
    {
        Task AddMessageAsync(string message, MessageCategory category = MessageCategory.Information, string details = null);
        Task RecordTestAsync(string testName, string testFramework, string filename, TestOutcome outcome, TimeSpan duration, string errorMessage = null, string errorStackTrace = null, string stdOut = null, string stdErr = null);


    }

    public enum MessageCategory
    {
        Information,
        Warning,
        Error
    }

    public enum TestOutcome
    {
        None,
        Running,
        Passed,
        Failed,
        Ignored,
        Skipped,
        Inconclusive,
        NotFound,
        Cancelled,
        NotRunnable
    }

    public enum ArtifactType
    {
        Auto,
        WebDeployPackage
    }
}
