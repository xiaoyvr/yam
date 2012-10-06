using System;

namespace Yam.Core
{
    public class CyclePathException : Exception
    {
        public CyclePathException(string message) : base(message)
        {
        }
    }
}