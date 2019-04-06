using Microsoft.Azure.WebJobs.Host;
using System;

namespace SmartDevelopment.Jobs.Base
{
    public class Activator : IJobActivator
    {
        private readonly IServiceProvider _container;

        public Activator(IServiceProvider container)
        {
            _container = container;
        }

        public T CreateInstance<T>()
        {
            return (T)_container.GetService(typeof(T));
        }
    }
}
