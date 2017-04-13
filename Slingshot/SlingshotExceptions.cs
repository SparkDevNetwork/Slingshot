using System;

namespace Slingshot
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.Exception" />
    internal abstract class SlingshotException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SlingshotException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SlingshotException( string message ) : base( message )
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Slingshot.SlingshotException" />
    internal class SlingshotEndpointNotFoundException : SlingshotException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SlingshotEndpointNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SlingshotEndpointNotFoundException( string message ) : base( message )
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Slingshot.SlingshotException" />
    internal class SlingshotPOSTFailedException : SlingshotException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SlingshotPOSTFailedException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SlingshotPOSTFailedException( RestSharp.IRestResponse restResponse ) :
            base( $"POST to {restResponse.Request.Resource}, StatusCode: {restResponse.StatusCode}, Content: {restResponse.Content} {restResponse.ErrorMessage}" )
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Slingshot.SlingshotException" />
    internal class SlingshotGETFailedException : SlingshotException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SlingshotGETFailedException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SlingshotGETFailedException( RestSharp.IRestResponse restResponse ) :
            base( $"GET from {restResponse.Request.Resource}, StatusCode: {restResponse.StatusCode}, Content: {restResponse.Content} {restResponse.ErrorMessage}" )
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Slingshot.SlingshotException" />
    internal class SlingshotLoginFailedException : SlingshotException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SlingshotLoginFailedException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public SlingshotLoginFailedException( string message ) : base( message )
        {
        }
    }
}