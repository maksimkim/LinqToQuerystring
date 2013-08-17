namespace ODataQuery.Exceptions
{
    using System;

    public class QueryParserException : Exception
    {
        public QueryParserException(string message)
            : base(message)
        {
            
        }
    }
}