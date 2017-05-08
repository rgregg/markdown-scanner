using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation
{
    public class MethodDuplicationException : Exception
    {
        public MethodDuplicationException(string message) : base(message)
        {

        }
    }
}
