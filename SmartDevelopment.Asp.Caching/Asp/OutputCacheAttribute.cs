using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace SmartDevelopment.Asp.Caching
{
    public class OutputCacheAttribute : Attribute, IAsyncActionFilter
    {
        protected bool _isUserSpecific;
        private readonly int _durationInSec;

        public bool IsCachable { get; set; } = true;

        public OutputCacheAttribute(bool isUserSpecific, int durationInSec)
        {
            _isUserSpecific = isUserSpecific;
            _durationInSec = durationInSec;
        }

        public virtual Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {            
            context.HttpContext.Items[Consts.IsCachebleKey] = IsCachable;
            context.HttpContext.Items[Consts.IsUserSpecificKey] = _isUserSpecific;
            context.HttpContext.Items[Consts.DurationKey] = _durationInSec;

            return next.Invoke();
        }
    }
}
