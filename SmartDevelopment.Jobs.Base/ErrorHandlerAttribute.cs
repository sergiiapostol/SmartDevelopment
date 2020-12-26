using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace SmartDevelopment.Jobs.Base
{
    public class ErrorHandlerAttribute : FunctionExceptionFilterAttribute
    {
        public override Task OnExceptionAsync(FunctionExceptionContext exceptionContext, CancellationToken cancellationToken)
        {
            var exception = exceptionContext.Exception;
            do
            {
                exceptionContext.Logger.LogError(exception,
                $"{exceptionContext.FunctionName}:{exceptionContext.FunctionInstanceId} failed with exception: {exception}");

                exception = exception.InnerException;
            } while (exception != null);

            return Task.CompletedTask;
        }
    }
}