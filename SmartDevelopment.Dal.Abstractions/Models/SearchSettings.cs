using System;
using System.Linq.Expressions;

namespace SmartDevelopment.Dal.Abstractions.Models
{
    public class SearchSettings<TEntity>
        where TEntity : IDbEntity
    {
        public SearchSettings(string search, string languageCode)
        {
            Search = search;
            LanguageCode = languageCode;
        }

        public SearchSettings(Expression<Func<TEntity, bool>> filter)
        {
            Filter = filter;
        }

        public Expression<Func<TEntity, bool>> Filter { get; set; }

        public string Search { get; }

        public string LanguageCode { get; }

        public bool FullMatch { get; set; }
    }
}
