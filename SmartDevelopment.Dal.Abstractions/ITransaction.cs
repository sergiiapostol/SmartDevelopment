using System;
using System.Threading.Tasks;

namespace SmartDevelopment.Dal.Abstractions
{
    public interface ITransaction : IDisposable
    {
        Task Commit();
        Task Rollback();
    }
}