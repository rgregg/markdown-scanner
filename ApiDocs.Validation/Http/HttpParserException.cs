using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.Http
{
    public class HttpParserException : ArgumentException
    {
        internal HttpParserException(string message) : base(message)
        {

        }
    }
}
