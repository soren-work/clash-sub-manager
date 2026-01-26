using System;
using System.Threading.Tasks;

namespace ClashSubManager.Services
{
    public interface IFileLockProvider
    {
        ValueTask<IAsyncDisposable> AcquireAsync(string filePath);
    }
}
