using System;
using Xunit;

namespace Hydrogen.Extensions.Mvc5.Async
{
    public static class AssertEx
    {
        public static TException Throws<TException>(Action testCode, string exceptionMessage)
            where TException : Exception
        {
            return Assert.Throws<TException>(testCode).AssertMessageEquals(exceptionMessage);
        }

        public static TException AssertMessageEquals<TException>(this TException exception, string exceptionMessage)
            where TException : Exception
        {
            Assert.Equal(exceptionMessage, exception.Message);
            return exception;
        }
    }
}
