using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ClashSubManager.Services
{
    public sealed class FileLockProvider : IFileLockProvider
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public async ValueTask<IAsyncDisposable> AcquireAsync(string filePath)
        {
            var key = NormalizeFilePath(filePath);
            var semaphore = _locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            return new Releaser(semaphore);
        }

        private static string NormalizeFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return string.Empty;

            var fullPath = Path.GetFullPath(filePath);
            return OperatingSystem.IsWindows() ? fullPath.ToUpperInvariant() : fullPath;
        }

        private sealed class Releaser : IAsyncDisposable
        {
            private SemaphoreSlim? _semaphore;

            public Releaser(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public ValueTask DisposeAsync()
            {
                Interlocked.Exchange(ref _semaphore, null)?.Release();
                return ValueTask.CompletedTask;
            }
        }
    }
}
