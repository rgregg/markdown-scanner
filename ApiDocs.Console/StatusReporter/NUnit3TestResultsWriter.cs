using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace ApiDocs.ConsoleApp.StatusReporter
{



    /// <summary>
    /// Sample: http://nunit.org/files/testresult_30.txt
    /// Format Docs: https://github.com/nunit/docs/wiki/Test-Result-XML-Format
    /// 
    /// </summary>
    internal class NUnit3TestResultsWriter : IReportingAgent
    {
        public string OutputPath { get; private set; }
        public NUnit3TestResultsWriter(string outputPath)
        {
            this.OutputPath = outputPath;
        }

        public Task AddMessageAsync(string message, MessageCategory category = MessageCategory.Information, string details = null)
        {
            throw new NotImplementedException();
        }

        public Task RecordTestAsync(string testName, string testFramework = null, string filename = null, TestOutcome outcome = TestOutcome.None, long durationInMilliseconds = 0, string errorMessage = null, string errorStackTrace = null, string stdOut = null, string stdErr = null)
        {
            throw new NotImplementedException();
        }


        private class TestSuite
        {
            [XmlAttribute("type")]
            public string Type { get; set; }

            [XmlAttribute("name")]
            public string Name { get; set; }

            [XmlAttribute("description")]
            public string Description { get; set; }

            [XmlAttribute("executed")]
            public bool Executed { get; set; }

            [XmlAttribute("result")]
            public string Result { get; set; }

            [XmlAttribute("success")]
            public bool Success { get; set; }

            [XmlAttribute("time")]
            public TimeSpan Time { get; set; }

            [XmlAttribute("asserts")]
            public int Asserts { get; set; }


            [XmlElement("results")]
            public TestResults Results { get; set; }

        }

        private class TestResults
        {
            [XmlElement("test-case")]
            public TestCase[] TestCases { get; set; }
        }

        private class TestCase
        {
            [XmlAttribute("name")]
            public string Name { get; set; }

            [XmlAttribute("executed")]
            public bool Executed { get; set; }

            [XmlAttribute("result")]
            public string Results { get; set; }

            [XmlAttribute("success")]
            public bool Success { get; set; }

            [XmlAttribute("time")]
            public TimeSpan Time { get; set; }

            [XmlAttribute("asserts")]
            public int Asserts { get; set; }

            [XmlElement("reason")]
            public CDataMessageElement Reason { get; set; }

            [XmlElement("failure")]
            public ResultFailure Failure { get; set; }
        }


        private class ResultFailure : CDataMessageElement
        {
            [XmlIgnore]
            public string StackTrace { get; set; }

            [XmlElement("stack-trace")]
            public XmlCDataSection CDataStackTrace
            {
                get
                {
                    XmlDocument doc = new XmlDocument();
                    return doc.CreateCDataSection(this.StackTrace);
                }
                set
                {
                    this.StackTrace = value.Value;
                }
            }
        }

        private class CDataMessageElement
        {
            [XmlIgnore]
            public string Message { get; set; }

            [XmlElement("message")]
            public XmlCDataSection MessageCData
            {
                get
                {
                    XmlDocument doc = new XmlDocument();
                    return doc.CreateCDataSection(this.Message);
                }
                set
                {
                    this.Message = value.Value;
                }
            }
        }



    }
}
