using System;
using System.Runtime.Serialization;

namespace Slingshot
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    internal class SlingshotEndpointNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SlingshotEndpointNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SlingshotEndpointNotFoundException(string message): base( message ) { }
    }
}