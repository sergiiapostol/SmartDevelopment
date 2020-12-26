using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SmartDevelopment.Dal.Abstractions.Models
{
    public class SortingSettings<TEntity>
    {
        SortingSettings()
        {
            Items = new List<SortingItem>();
        }

        public SortingSettings(SortingItem sorting) : this()
        {
            Items.Add(sorting);
        }

        public SortingSettings(params SortingItem[] sortingitems) : this()
        {
            Items.AddRange(sortingitems);
        }

        public List<SortingItem> Items { get; }

        public SortingSettings<TEntity> Asc(Expression<Func<TEntity, object>> expression)
        {
            Items.Add(new SortingItem(true, expression));

            return this;
        }

        public SortingSettings<TEntity> Desc(Expression<Func<TEntity, object>> expression)
        {
            Items.Add(new SortingItem(false, expression));

            return this;
        }

        public class SortingItem
        {
            public SortingItem(bool asc, Expression<Func<TEntity, object>> expression)
            {
                Asc = asc;
                Expression = expression;
            }

            public bool Asc { get; }

            public Expression<Func<TEntity, object>> Expression { get; }
        }
    }
}
