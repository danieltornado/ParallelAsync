# ParallelAsync

# Sample:

``` c#
    ParallelOptionsAsync options = new ParallelOptionsAsync();
    options.MaxParallelThreads = 4;

    // ���������� ���������� ������ �� ����������. �� ������ ����� ����� ��� �� ����������.
    IEnumerable<Task<int>> jobs = Enumerable.Range(0, 20).Select(i => Task.Run<int>(() => { Thread.Sleep(1000); return i; }));

    ParallelEnumeratorAsync<Task<int>> enumerator = new ParallelEnumeratorAsync<Task<int>>(jobs, options);
    // � �������� �������� ������� �������������� �����, ����� 4 ���������� ������.
    // �� ���������� �����, ���� ������� ��������� ���������� ������ ���������� ������� ���������� ����� ������.
    while(await enumerator.MoveNextAsync())
    {
        // ��������� ��������� �� ���������. ��������������, ��� ��� �� �������� ����� �������.
        PushResult(enumerator.Current);
    }
```