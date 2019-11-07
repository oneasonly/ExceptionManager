using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionManager
{
    public class CustException : Exception
    {
        public CustException() : base()
        { }

        public CustException(string message) : base(message)
        { }

        public CustException(string message, Exception innerException) : base(message, innerException)
        { }

        [SecuritySafeCritical]
        protected CustException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }
}
