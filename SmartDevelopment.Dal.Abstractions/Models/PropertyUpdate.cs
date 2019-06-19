using System;
using System.Linq.Expressions;
using SmartDevelopment.Dal.Abstractions;

namespace SmartDevelopment.Dal.Abstractions.Models
{
    public class PropertyUpdate<TEntity>
        where TEntity : class, IDbEntity
    {
        public PropertyUpdate(Expression<Func<TEntity, object>> property, object value)
        {
            Property = property;
            Value = value;
        }

        public Expression<Func<TEntity, object>> Property { get; }

        public object Value { get; }
    }
}