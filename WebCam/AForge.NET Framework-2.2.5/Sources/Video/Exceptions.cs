// AForge Video Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2005-2011
// contacts@aforgenet.com
//

namespace AForge.Video
{
    using System;

    /// <summary>
    /// Video related exception.
    /// </summary>
    /// 
    /// <remarks><para>The exception is thrown in the case of some video related issues, like
    /// failure of initializing codec, compression, etc.</para></remarks>
    /// 
    [Serializable]
    public class VideoException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoException"/> class.
        /// </summary>
        /// 
        /// <param name="message">Exception's message.</param>
        /// 
        public VideoException( string message ) : base( message )
        {
        }

        protected VideoException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
        }

        public VideoException()
        {
        }

        public VideoException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
