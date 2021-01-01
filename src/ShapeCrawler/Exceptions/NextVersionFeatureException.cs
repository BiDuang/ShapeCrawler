﻿using ShapeCrawler.Enums;

namespace ShapeCrawler.Exceptions
{
    /// <summary>
    /// Thrown when a feature not yet been implemented.
    /// </summary>
    public class NextVersionFeatureException : SlideDotNetException
    {
        public NextVersionFeatureException(string message) 
            : base(message, ExceptionCodes.NextVersionFeatureException) { }

        public NextVersionFeatureException()
        {
        }

        public NextVersionFeatureException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}