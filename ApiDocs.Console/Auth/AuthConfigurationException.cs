using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.ConsoleApp.Auth
{
    public class AuthConfigurationException : Exception
    {

        internal AuthConfigurationException(string message) : base(message)
        {

        }

    }
}
