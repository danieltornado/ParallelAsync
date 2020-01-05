using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ParallelAsync.Enumeration
{
    public class ParallelOptionsAsync
    {
        public int MaxParallelThreads { get; set; }
        public CancellationToken Token { get; set; }
    }
}
