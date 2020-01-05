using System.Threading.Tasks;

namespace ParallelAsync.Enumeration
{
    internal struct BufferBlock<T>
        where T : Task
    {
        public BufferBlock(T job)
        {
            HasNext = true;
            Task = job;
        }

        public T Task { get; }

        public bool HasNext { get; }
    }
}
