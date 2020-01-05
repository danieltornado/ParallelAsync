using System.Collections.Generic;
using System.Threading.Tasks;

namespace ParallelAsync.Enumeration
{
    public class ParallelEnumeratorAsync<T>
        where T : Task
    {
        #region States

        /// <summary>
        /// IAsyncEnumerator do not exist.
        /// </summary>
        private interface IState
        {
            Task<bool> MoveNextAsync();

            T Current { get; }

            Task DisposeAsync();
        }

        private class PendingState : IState
        {
            private readonly ParallelEnumeratorAsync<T> _owner;
            private readonly IEnumerable<T> _jobs;
            private readonly ParallelOptionsAsync _options;

            public PendingState(ParallelEnumeratorAsync<T> owner, IEnumerable<T> jobs, ParallelOptionsAsync options)
            {
                _owner = owner;
                _jobs = jobs;
                _options = options;
            }

            public T Current => default(T);
            
            public Task<bool> MoveNextAsync()
            {
                _owner._state = new RunningState(_jobs, _options);
                return _owner._state.MoveNextAsync();
            }

            public Task DisposeAsync()
            {
                return Task.CompletedTask;
            }
        }

        private class RunningState : IState
        {
            private readonly Buffer<T> _buffer;

            public RunningState(IEnumerable<T> jobs, ParallelOptionsAsync options)
            {
                _buffer = new Buffer<T>(jobs.GetEnumerator(), options);
                _buffer.Run();
            }

            public Task<bool> MoveNextAsync()
            {
                return _buffer.GetCompleted().ContinueWith(t =>
                {
                    Current = t.Result.Task;
                    return t.Result.HasNext;
                }, TaskContinuationOptions.NotOnCanceled);
            }

            public T Current { get; private set; }

            public Task DisposeAsync()
            {
                _buffer.Dispose();
                return Task.CompletedTask;
            }
        }

        #endregion

        private IState _state;

        public ParallelEnumeratorAsync(IEnumerable<T> jobs, ParallelOptionsAsync options)
        {
            _state = new PendingState(this, jobs, options);
        }

        public Task DisposeAsync()
        {
            return _state.DisposeAsync();
        }

        public Task<bool> MoveNextAsync()
        {
            return _state.MoveNextAsync();
        }

        public T Current => _state.Current;
    }
}
