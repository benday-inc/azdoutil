using System;
using System.Linq;
public class KnownException : Exception
{
    public KnownException(string message) : base(message) { }

}