using SmartDevelopment.Dal.Abstractions.Models;
using System;

namespace SmartDevelopment.Dal.Abstractions.Exceptions
{
    public class InsertOrUpdateException : Exception
    {
        public InsertOrUpdateException(InsertOrUpdateResult result, Exception innerException)
            : base(string.Empty, innerException)
        {
            Result = result;
        }

        public InsertOrUpdateResult Result { get; }
    }
}