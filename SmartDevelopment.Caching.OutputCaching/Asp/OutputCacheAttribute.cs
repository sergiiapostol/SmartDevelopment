using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace SmartDevelopment.Caching.OutputCaching
{
    public class OutputCacheAttribute : Attribute, IAsyncActionFilter
    {
        protected bool _isUserSpecific;

        public bool IsCachable { get; set; } = true;

        public OutputCacheAttribute(bool isUserSpecific)
        {
            _isUserSpecific = isUserSpecific;
        }

        public int DurationInSec { get; set; }

        public int SlidingDurationInSec { get; set; }

        public virtual Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {            
            context.HttpContext.Items[Consts.IsCachebleKey] = IsCachable;
            context.HttpContext.Items[Consts.IsUserSpecificKey] = _isUserSpecific;
            if(DurationInSec > 0)
                context.HttpContext.Items[Consts.DurationKey] = DurationInSec;
            if (SlidingDurationInSec > 0)
                context.HttpContext.Items[Consts.SlidingDurationKey] = SlidingDurationInSec;

            return next.Invoke();
        }
    }
}
