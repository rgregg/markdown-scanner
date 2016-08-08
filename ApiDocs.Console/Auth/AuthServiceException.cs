using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.ConsoleApp.Auth
{
    public class AuthServiceException : Exception
    {
        internal AuthServiceException(string message) : base(message)
        {

        }
    }
}
