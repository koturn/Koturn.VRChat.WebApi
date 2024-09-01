using System;
using System.Net.Http;

namespace Koturn.VRChat.WebApi.Events
{
    /// <summary>
    /// Custom event argument
    /// </summary>
    public class HttpRequestEventArgs : EventArgs
    {
        public HttpRequestMessage Request { get; }

        /// <summary>
        /// Initialize all members.
        /// </summary>
        public HttpRequestEventArgs(HttpRequestMessage request)
        {
            Request = request;
        }
    }
}
