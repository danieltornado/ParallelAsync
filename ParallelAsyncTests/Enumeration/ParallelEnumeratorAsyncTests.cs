using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParallelAsync.Enumeration;
using Xunit;
using Xunit.Abstractions;

namespace ParallelAsyncTests.Enumeration
{
    public class ParallelEnumeratorAsyncTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ParallelEnumeratorAsyncTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        #region Simple test

        [Fact]
        public async Task ParallelEnumeratorAsync_Simple_Execute()
        {
            var enumerator = new ParallelEnumeratorAsync<Task<int>>(GetTestTasks(), new ParallelOptionsAsync { MaxParallelThreads = 2 });

            _testOutputHelper.WriteLine("Jobs starting");

            while (await enumerator.MoveNextAsync())
            {
                var task = enumerator.Current;
                _testOutputHelper.WriteLine($"Task get result: {task.Result}");
            }

            _testOutputHelper.WriteLine("Jobs completed");
        }

        private IEnumerable<Task<int>> GetTestTasks()
        {
            _testOutputHelper.WriteLine("Task 1 requested");
            yield return Task.Delay(5000).ContinueWith(t =>
            {
                _testOutputHelper.WriteLine("Task 1 finished");
                return 1;
            });

            _testOutputHelper.WriteLine("Task 2 requested");
            yield return Task.Delay(5000).ContinueWith(t =>
            {
                _testOutputHelper.WriteLine("Task 2 finished");
                return 2;
            });

            _testOutputHelper.WriteLine("Task 3 requested");
            yield return Task.Delay(5000).ContinueWith(t =>
            {
                _testOutputHelper.WriteLine("Task 3 finished");
                return 3;
            });

            _testOutputHelper.WriteLine("Task 4 requested");
            yield return Task.Delay(2000).ContinueWith(t =>
            {
                _testOutputHelper.WriteLine("Task 4 finished");
                return 4;
            });

            _testOutputHelper.WriteLine("Task 5 requested");
            yield return Task.Delay(1000).ContinueWith(t =>
            {
                _testOutputHelper.WriteLine("Task 5 finished");
                return 5;
            });

            _testOutputHelper.WriteLine("Task 6 requested and finished");
            yield return Task.FromResult(6);
        }

        #endregion

        #region Next test

        [Fact]
        public async Task ParallelEnumeratorAsync_Next_Execute()
        {
            var range = Enumerable.Range(0, 20).ToList();
            var indexList = range.ToList();
            var jobs = range.Select(i =>
            {
                _testOutputHelper.WriteLine($"Task {i} requested");
                return Task.Delay(5000 - 100 * i).ContinueWith(t => i);
            });

            var enumerator = new ParallelEnumeratorAsync<Task<int>>(jobs, new ParallelOptionsAsync { MaxParallelThreads = 2 });

            _testOutputHelper.WriteLine("Jobs starting");

            while (await enumerator.MoveNextAsync())
            {
                var task = enumerator.Current;
                _testOutputHelper.WriteLine($"Task get result: {task.Result}");

                Assert.Contains(indexList, i => task.Result == i);
                indexList.Remove(task.Result);
            }

            Assert.Empty(indexList);
            _testOutputHelper.WriteLine("Jobs completed");
        }

        #endregion

        #region Cancel test

        [Fact]
        public async Task ParallelEnumeratorAsync_Cancel()
        {
            var tokenSource = new CancellationTokenSource();
            var enumerator = new ParallelEnumeratorAsync<Task<int>>(GetTestTasks_ForCancel(tokenSource), new ParallelOptionsAsync { MaxParallelThreads = 3, Token = tokenSource.Token });
            int completed = 0;

            _testOutputHelper.WriteLine("Jobs starting");

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                while (await enumerator.MoveNextAsync())
                {
                    var task = enumerator.Current;
                    _testOutputHelper.WriteLine($"Task get result: {task.Result}");
                    completed++;
                }
            });

            _testOutputHelper.WriteLine("Jobs completed");

            // No more 2 result
            Assert.True(completed <= 2);
        }

        private IEnumerable<Task<int>> GetTestTasks_ForCancel(CancellationTokenSource tokenSource)
        {
            _testOutputHelper.WriteLine("Task 1 requested");
            yield return Task.Delay(5000).ContinueWith(t =>
            {
                _testOutputHelper.WriteLine("Task 1 finished");
                return 1;
            });

            _testOutputHelper.WriteLine("Task 2 requested");
            yield return Task.Delay(5000).ContinueWith(t =>
            {
                _testOutputHelper.WriteLine("Task 2 finished");
                return 2;
            });

            // But next task will be requested.
            tokenSource.Cancel();

            _testOutputHelper.WriteLine("Task 3 requested");
            yield return Task.Delay(5000).ContinueWith(t =>
            {
                _testOutputHelper.WriteLine("Task 3 finished");
                return 3;
            });

            _testOutputHelper.WriteLine("Task 4 requested");
            yield return Task.Delay(2000).ContinueWith(t =>
            {
                _testOutputHelper.WriteLine("Task 4 finished");
                return 4;
            });

            _testOutputHelper.WriteLine("Task 5 requested");
            yield return Task.Delay(1000).ContinueWith(t =>
            {
                _testOutputHelper.WriteLine("Task 5 finished");
                return 5;
            });

            _testOutputHelper.WriteLine("Task 6 requested and finished");
            yield return Task.FromResult(6);
        }

        #endregion
    }
}