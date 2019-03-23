using System;

namespace SmartDevelopment.Dal.Abstractions.Exceptions
{
    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(Type type, object id) : base(
            $"Entity type:'{type.FullName}' id:'{id}; was not found")
        { }
    }
}
