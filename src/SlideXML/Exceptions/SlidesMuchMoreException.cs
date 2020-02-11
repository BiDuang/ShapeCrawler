﻿using SlideXML.Enums;

namespace SlideXML.Exceptions
{
    /// <summary>
    /// Thrown when number of slides more than allowed.
    /// </summary>
    public class SlidesMuchMoreException : SlideXmlException
    {
        #region Constructors

        private SlidesMuchMoreException(string message) : base(message, (int)ExceptionCodes.SlidesMuchMoreException) { }

        #endregion Constructors

        public static SlidesMuchMoreException FromMax(int maxNum)
        {
            var message = ExceptionMessages.SlidesMuchMore.Replace("{0}", maxNum.ToString());
            return new SlidesMuchMoreException(message);
        }
    }
}