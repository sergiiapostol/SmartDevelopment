using System.Threading.Tasks;

namespace SmartDevelopment.Dal.Abstractions
{
    public interface IIndexedSource
    {
        Task EnsureIndex();
    }
}