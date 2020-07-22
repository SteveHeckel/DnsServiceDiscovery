using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mittosoft.DnsServiceDiscovery
{
    public sealed class AsyncManualResetEvent
    {
        private volatile TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();

        public Task<bool> WaitAsync(CancellationToken token = default)
        {
            return WaitAsync(-1, token);
        }

        public async Task<bool> WaitAsync(int milliseconds, CancellationToken token = default)
        {
            if (milliseconds < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(milliseconds));
            }

            CancellationTokenSource timeout;

            if (false == token.CanBeCanceled)
            {
                if (milliseconds == -1)
                {
                    return await _completionSource.Task;
                }

                timeout = new CancellationTokenSource();
            }
            else
            {
                timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            }

            using (timeout)
            {
                var delay = Task.Delay(milliseconds, timeout.Token).ContinueWith((result) =>
                {
                    var e = result.Exception;
                }, TaskContinuationOptions.ExecuteSynchronously);

                var resulting = await Task.WhenAny(_completionSource.Task, delay).ConfigureAwait(false);

                if (resulting != delay)
                {
                    timeout.Cancel();
                    return true;
                }

                token.ThrowIfCancellationRequested();
                return false;
            }
        }

        public void Set()
        {
            Task.Run(() => _completionSource.TrySetResult(true));
        }

        public void Reset()
        {
            var currentCompletionSource = _completionSource;

            if (!currentCompletionSource.Task.IsCompleted)
            {
                return;
            }

            Interlocked.CompareExchange(ref _completionSource, new TaskCompletionSource<bool>(), currentCompletionSource);
        }
    }
}
