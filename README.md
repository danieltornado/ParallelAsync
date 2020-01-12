# ParallelAsync

# Sample:

``` c#
    ParallelOptionsAsync options = new ParallelOptionsAsync();
    options.MaxParallelThreads = 4;

    // Возвращает запущенную задачу по требованию. На данном этапе задач ещё не существует.
    IEnumerable<Task<int>> jobs = Enumerable.Range(0, 20).Select(i => Task.Run<int>(() => { Thread.Sleep(1000); return i; }));

    ParallelEnumeratorAsync<Task<int>> enumerator = new ParallelEnumeratorAsync<Task<int>>(jobs, options);
    // В процессе ожидания первого завершившегося таска, имеем 4 запущенные задачи.
    // Не удерживаем поток, если процесс обработки результата задачи происходит быстрее выполнения самой задачи.
    while(await enumerator.MoveNextAsync())
    {
        // Отправить результат на обработку. Предполагается, что она не занимает много времени.
        PushResult(enumerator.Current);
    }
```