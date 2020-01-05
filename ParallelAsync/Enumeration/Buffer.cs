using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelAsync.Enumeration
{
    internal sealed class Buffer<T> : IDisposable
        where T : Task
    {
        private readonly object _sync;
        private readonly IEnumerator<T> _jobsEnumerator;
        private readonly ParallelOptionsAsync _options;
        private readonly LinkedList<BufferBlock<T>> _postResultBuffer;

        private TaskCompletionSource<BufferBlock<T>> _preResultAwaiter;
        private CancellationToken _token;
        private CancellationTokenRegistration _registration;
        private volatile int _executingCount;

        public Buffer(IEnumerator<T> jobsEnumerator, ParallelOptionsAsync options)
        {
            _sync = new object();
            _jobsEnumerator = jobsEnumerator;
            _postResultBuffer = new LinkedList<BufferBlock<T>>();
            _options = options;
            _executingCount = 0;

            _token = options.Token;
            _registration = _token.Register(OnCancel);
        }

        public void Run()
        {
            // Запуск
            lock (_sync)
            {
                for (int i = 0; i < _options.MaxParallelThreads; i++)
                {
                    if (!PushNextInner())
                        break;
                }
            }
        }

        public Task<BufferBlock<T>> GetCompleted()
        {
            lock (_sync)
            {
                if (_token.IsCancellationRequested)
                    return Task.FromCanceled<BufferBlock<T>>(_token);

                if (_postResultBuffer.Count > 0)
                {
                    var result = _postResultBuffer.First.Value;
                    _postResultBuffer.RemoveFirst();
                    return Task.FromResult(result);
                }

                // Окончание процесса
                if (_executingCount == 0)
                    return Task.FromResult(default(BufferBlock<T>));

                _preResultAwaiter = new TaskCompletionSource<BufferBlock<T>>();
                return _preResultAwaiter.Task;
            }
        }

        private bool PushNextInner()
        {
            // Don't read next task, if cancellation is requested.
            if (_token.IsCancellationRequested)
                return false;

            if (_jobsEnumerator.MoveNext())
            {
                Debug.Assert(_jobsEnumerator.Current != null, "Создаваемая задача не должна быть null.");

                _executingCount++;
                _jobsEnumerator.Current.ContinueWith(OnTaskCompleted);
                return true;
            }

            return false;
        }

        private void OnTaskCompleted(Task task)
        {
            lock (_sync)
            {
                _executingCount--;

                PushNextInner();

                var result = new BufferBlock<T>((T)task);
                if (_preResultAwaiter != null)
                {
                    _preResultAwaiter.SetResult(result);
                    _preResultAwaiter = null;
                }
                else
                {
                    _postResultBuffer.AddLast(result);
                }
            }
        }

        private void OnCancel()
        {
            lock (_sync)
            {
                _preResultAwaiter?.SetCanceled();
            }
        }

        public void Dispose()
        {
            _registration.Dispose();
        }
    }
}
