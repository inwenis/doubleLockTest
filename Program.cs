using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace doubleLockTest
{
    class Program
    {
        private static bool _testIsRunning = true;

        static void Main(string[] args)
        {
            _testIsRunning = true;
            Console.WriteLine("Running test using single lock");
            RunTest(new ProcessorCacheWithSingleLock());

            //let the threads die
            Console.WriteLine("waitig 5 secondos for all threads to die");
            Thread.Sleep(5000);
            Console.Clear();

            _testIsRunning = true;
            Console.WriteLine("Running test using double lock");
            RunTest(new ProcessorCacheWithDoubleLock());

            Console.WriteLine("Press [enter] to exit");
            Console.ReadLine();
        }

        private static void RunTest(IProcessorCache cache)
        {
            foreach (var i in Enumerable.Range(0, 10000))
            {
                cache.Add(i, new Processor {Name = i.ToString()});
            }

            var readers = Enumerable
                .Range(0, 20)
                .Select(x => new Thread(ReadProcessors));

            foreach (var reader in readers)
            {
                reader.Start(cache);
            }

            var remover = new Thread(RemoveProessors);
            remover.Start(cache);

            while (_testIsRunning)
            {
            }

            remover.Abort();
            foreach (var reader in readers)
            {
                reader.Abort();
            }
        }

        private static void ReadProcessors(object data)
        {
            Console.WriteLine("reader will try to get the 10th entry");
            var cache = (IProcessorCache) data;

            while (true)
            {
                try
                {
                    var processor = cache.Get(10);
                    if (processor == null)
                    {
                        break;
                    }
                }
                catch (KeyNotFoundException e)
                {
                    Console.WriteLine("the key not found exception was thrown");
                    Console.Beep();
                    _testIsRunning = false;
                    break;
                }
                catch (LockRecursionException)
                {
                    break;
                }
            }
        }

        private static void RemoveProessors(object data)
        {
            Console.WriteLine("remover will remove entries from the cache");
            var cache = (IProcessorCache) data;

            var random = new Random(DateTime.Now.Millisecond);

            for (int i = 0; i <= 10000; i++)
            {
                cache.Remove(i);
                Console.SetCursorPosition(0, 25);
                Console.Write($"remover: removed entry: {i,4}, entries left: {cache.Count()}    ");
            }

            _testIsRunning = false;
            Console.WriteLine("\nthere are no more entreis to remove, the remover will not exit");
        }

    }

    internal interface IProcessorCache
    {
        bool Exists(int id);
        void Add(int id, Processor processor);
        Processor Get(int id);
        void Remove(int id);
        int Count();
    }

    class ProcessorCacheWithSingleLock : IProcessorCache
    {
        private Dictionary<int, Processor> _dictionary;
        private ReaderWriterLockSlim _lock;

        public ProcessorCacheWithSingleLock()
        {
            _dictionary = new Dictionary<int, Processor>();
            _lock = new ReaderWriterLockSlim();
        }

        public bool Exists(int id)
        {
            _lock.EnterReadLock();
            var containsKey = _dictionary.ContainsKey(id);
            _lock.ExitReadLock();
            return containsKey;
        }

        public void Add(int id, Processor processor)
        {
            _dictionary.Add(id, processor);
        }

        public Processor Get(int id)
        {
            if (Exists(id))
            {
                _lock.EnterReadLock();
                var processor = _dictionary[id];
                _lock.ExitReadLock();
                return processor;
            }
            else
            {
                return null;
            }
        }

        public void Remove(int id)
        {
            _lock.EnterWriteLock();
            _dictionary.Remove(id);
            _lock.ExitWriteLock();
        }

        public int Count()
        {
            _lock.EnterReadLock();
            var dictionaryCount = _dictionary.Count;
            _lock.ExitReadLock();
            return dictionaryCount;
        }
    }

    class ProcessorCacheWithDoubleLock : IProcessorCache
    {
        private Dictionary<int, Processor> _dictionary;
        private ReaderWriterLockSlim _lock;

        public ProcessorCacheWithDoubleLock()
        {
            _dictionary = new Dictionary<int, Processor>();
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        public bool Exists(int id)
        {
            _lock.EnterReadLock();
            var containsKey = _dictionary.ContainsKey(id);
            _lock.ExitReadLock();
            return containsKey;
        }

        public void Add(int id, Processor processor)
        {
            _dictionary.Add(id, processor);
        }

        public Processor Get(int id)
        {
            _lock.EnterReadLock();
            {
                if (Exists(id))
                {
                    var processor = _dictionary[id];
                    _lock.ExitReadLock();
                    return processor;
                }
                else
                {
                    _lock.ExitReadLock();
                    return null;
                }
            }
        }

        public void Remove(int id)
        {
            _lock.EnterWriteLock();
            _dictionary.Remove(id);
            _lock.ExitWriteLock();
        }

        public int Count()
        {
            _lock.EnterReadLock();
            var dictionaryCount = _dictionary.Count;
            _lock.ExitReadLock();
            return dictionaryCount;
        }
    }

    public class Processor
    {
        public string Name;
    }
}
