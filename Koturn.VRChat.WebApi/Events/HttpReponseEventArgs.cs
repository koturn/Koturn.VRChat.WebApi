using System;
using System.Net.Http;


namespace Koturn.VRChat.WebApi.Events
{
    /// <summary>
    /// Custom event argument
    /// </summary>
    public class HttpResponseEventArgs : EventArgs
    {
        /// <summary>
        /// Response.
        /// </summary>
        public HttpResponseMessage Response { get; }


        /// <summary>
        /// Initialize all members.
        /// </summary>
        public HttpResponseEventArgs(HttpResponseMessage response)
        {
            Response = response;
        }
    }
}
