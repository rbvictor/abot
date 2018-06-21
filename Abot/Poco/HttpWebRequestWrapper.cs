using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;

namespace Abot.Poco
{
    public class HttpWebRequestWrapper : HttpWebRequest
    {
        protected HttpWebRequestWrapper(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}
