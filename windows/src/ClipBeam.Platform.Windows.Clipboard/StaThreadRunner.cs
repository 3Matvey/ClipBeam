using System.Collections.Concurrent;

namespace ClipBeam.Platform.Windows.Clipboard
{
    internal sealed class StaThreadRunner : IDisposable
    {
        private volatile bool _stopping;

        private readonly Thread _thread;
        private readonly BlockingCollection<(Func<object?> Work, TaskCompletionSource<object?> Tcs)> _queue
            = new(new ConcurrentQueue<(Func<object?>, TaskCompletionSource<object?>)>());

        public StaThreadRunner()
        {
            _thread = new Thread(RunLoop)
            {
                IsBackground = true,
                Name = "ClipBeam.STA"
            };
#pragma warning disable CA1416
            _thread.SetApartmentState(ApartmentState.STA);
#pragma warning restore CA1416
            _thread.Start();
        }

        public Task RunAsync(Action action, CancellationToken ct = default)
            => RunAsync<object?>(() =>
            {
                action();
                return null;
            }, ct);

        public Task<T> RunAsync<T>(Func<T> func, CancellationToken ct = default)
        {
            if (_stopping)
                throw new ObjectDisposedException(nameof(StaThreadRunner));

            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            CancellationTokenRegistration ctr = default;
            if (ct.CanBeCanceled)
                ctr = ct.Register(() => tcs.TrySetCanceled(ct));

            try
            {
                _queue.Add((() => func()!, tcs), ct);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled(ct);
                ctr.Dispose();
                throw;
            }
            catch
            {
                tcs.TrySetException(new InvalidOperationException("Failed to enqueue work item."));
                ctr.Dispose();
                throw;
            }

            return tcs.Task.ContinueWith(t =>
            {
                ctr.Dispose();

                if (t.IsCanceled) throw new TaskCanceledException(t);
                if (t.IsFaulted) throw t.Exception!.InnerException!;

                return (T?)t.Result!;
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)!;
        }

        private void RunLoop()
        {
            try
            {
                foreach (var (work, tcs) in _queue.GetConsumingEnumerable())
                {
                    if (_stopping)
                    {
                        tcs.TrySetCanceled();
                        continue;
                    }

                    try
                    {
                        var result = work();
                        tcs.TrySetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                while (_queue.TryTake(out var item))
                    item.Tcs.TrySetException(ex);
            }
        }

        public void Dispose()
        {
            _stopping = true;
            _queue.CompleteAdding();

            if (_thread.IsAlive)
                _thread.Join(TimeSpan.FromSeconds(1));

            _queue.Dispose();
        }
    }
}
