using System;

namespace Yam.Core.Graph
{
    public class CyclePathException : Exception
    {
        public CyclePathException(string message) : base(message)
        {
        }
    }
}