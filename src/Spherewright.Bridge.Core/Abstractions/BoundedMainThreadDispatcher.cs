using System.Diagnostics;

namespace Spherewright.Bridge.Core.Abstractions;

public sealed class BoundedMainThreadDispatcher : IDisposable
{
    private readonly object _gate = new object();
    private readonly Queue<IWorkItem> _queue = new Queue<IWorkItem>();
    private readonly int _capacity;
    private bool _disposed;

    public BoundedMainThreadDispatcher(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _capacity = capacity;
    }

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _queue.Count;
            }
        }
    }

    public bool TryEnqueue<T>(Func<T> operation, out Task<T> completion)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        var item = new WorkItem<T>(operation);
        lock (_gate)
        {
            if (_disposed || _queue.Count >= _capacity)
            {
                completion = item.Task;
                item.Cancel();
                return false;
            }

            _queue.Enqueue(item);
            completion = item.Task;
            return true;
        }
    }

    public int Pump(int maxItems, TimeSpan budget)
    {
        if (maxItems <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxItems));
        }

        if (budget <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(budget));
        }

        var stopwatch = Stopwatch.StartNew();
        var executed = 0;
        while (executed < maxItems && stopwatch.Elapsed < budget)
        {
            IWorkItem? item;
            lock (_gate)
            {
                item = _queue.Count > 0 ? _queue.Dequeue() : null;
            }

            if (item is null)
            {
                break;
            }

            item.Execute();
            executed++;
        }

        return executed;
    }

    public void Dispose()
    {
        IWorkItem[] pending;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            pending = _queue.ToArray();
            _queue.Clear();
        }

        foreach (var item in pending)
        {
            item.Cancel();
        }
    }

    private interface IWorkItem
    {
        void Execute();

        void Cancel();
    }

    private sealed class WorkItem<T> : IWorkItem
    {
        private readonly Func<T> _operation;
        private readonly TaskCompletionSource<T> _completion =
            new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        public WorkItem(Func<T> operation)
        {
            _operation = operation;
        }

        public Task<T> Task => _completion.Task;

        public void Execute()
        {
            try
            {
                _completion.TrySetResult(_operation());
            }
            catch (Exception exception)
            {
                _completion.TrySetException(exception);
            }
        }

        public void Cancel()
        {
            _completion.TrySetCanceled();
        }
    }
}
