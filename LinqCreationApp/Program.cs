// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections;
using System.Diagnostics;

Console.WriteLine("Hello, World!");

/* Part 1 **************************************************
IEnumerable<int> e = GetValues();
using IEnumerator<int> eEnumerator = e.GetEnumerator();
// Implementation of IEnumerator is provided by compiler
Console.WriteLine(eEnumerator);
while (eEnumerator.MoveNext())
{

    Console.WriteLine(eEnumerator.Current);
}

// foreach is higher level construct
foreach (var item in GetValues())
{
    Console.WriteLine(item);
}

static IEnumerable<int> GetValues()
{
    // Iterators do the resumption
    yield return 1;
    yield return 2;
    yield return 3;
}
************************************************************/

/* Part 2 ***************************************************
IEnumerable<Person> persons = new List<Person>
{ new Person {Id = 1 },new Person {Id = 2 },new Person {Id = 3 } };

IEnumerable<int> Ids = persons.Select(p => p.Id);
class Person
{
    public int Id { get; set; }
}
*************************************************************/


//IEnumerable<int> source = new[] {1,2,3};
//foreach (var item in Select(source, i => i*2))
//{
//    Console.WriteLine(item);
//}

// For Enumrator, zero code from bosy will be called until MoveNext()
// Here is an example
//Enumerable.Select<int,int>(null, i => i*2); // With Exception
//Select<int, int>(null, i => i * 2);             // Without Exception


//Console.WriteLine(0);
//IEnumerable<int> e = Select<int, int>(null, i => i * 2);
//Console.WriteLine(1);
//IEnumerator<int> enumerator = e.GetEnumerator();
//Console.WriteLine(2);
//enumerator.MoveNext();

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

//To see how many different iterators are implemented
//behind the scene by Linq
//based on the context data they have
//like which Linq pattern people use most 
//IEnumerable<int> source = Enumerable.Range(0, 1000).ToArray();
//Console.WriteLine(Tests.SelectCompiler(source, x => x * 2));
//Console.WriteLine(Tests.SelectManual(source, x => x * 2));
//Console.WriteLine(Enumerable.Select(new List<int>(), x => x * 2));
//Console.WriteLine(Enumerable.Select(new Queue<int>(), x => x * 2));
//Console.WriteLine(source.Where(x => x % 2 == 0).Select(i => i));



[MemoryDiagnoser]
[ShortRunJob]
public class Tests
{
    [Benchmark]
    public int SumCompiler()
    {
        int result = 0;
        foreach (var i in SelectCompiler(source, i => i * 2))
        {
            result += i;
        }

        return result;
    }

    [Benchmark]
    public int SumManual()
    {
        int result = 0;
        foreach (var i in SelectManual(source, i => i * 2))
        {
            result += i;
        }

        return result;
    }


    [Benchmark]
    public int SumLinq()
    {
        int result = 0;
        foreach (var i in Enumerable.Select(source, i => i * 2))
        {
            result += i;
        }

        return result;
    }


    private IEnumerable<int> source = Enumerable.Range(0, 1000).ToArray();

    //for (int i = 0; i < 10_000; i++)
    //{
    //    // Performance Profiler shows objects allocation for IEnumerables
    //    // (object size will be large as it will combine IEnumerable and IEnumerator in single object) 
    //    //foreach (var item in SelectCompiler(source, x => x * 2))
    //    // Performance Profiler shows objects allocation for IEnumerables and IEnumerators 
    //    // (object size will be small but no of objects will be doubled)
    //    foreach (var item in SelectManual(source, x => x * 2))
    //    {

    //    }
    //}

    //Console.WriteLine(Enumerable.Select(source, x => x * 2).Sum());
    //var l = SelectCompiler(source, x => x * 2);
    //Console.WriteLine(l.Sum());
    //Console.WriteLine(l.Sum());
    //var m = SelectManual(source, x => x * 2);
    //Console.WriteLine(m.Sum());
    //Console.WriteLine(m.Sum());


    public static IEnumerable<TResult> SelectCompiler<TSource, TResult>(
        IEnumerable<TSource> source,
        Func<TSource, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return Impl(source, selector);

        static IEnumerable<TResult> Impl<TSource, TResult>(
        IEnumerable<TSource> source,
        Func<TSource, TResult> selector)
        {
            foreach (var item in source)
            {
                yield return selector(item);
            }
        }
    }

    public static IEnumerable<TResult> SelectManual<TSource, TResult>(
        IEnumerable<TSource> source,
        Func<TSource, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        //return Impl(source, selector);
        return new SelectManualEnumerable<TSource, TResult>(source, selector);

        //static IEnumerable<TResult> Impl<TSource, TResult>(
        //IEnumerable<TSource> source,
        //Func<TSource, TResult> selector)
        //{
        //    foreach (var item in source)
        //    {
        //        yield return selector(item);
        //    }
        //}
    }

    sealed class SelectManualEnumerable<TSource, TResult>
        : IEnumerable<TResult>, IEnumerator<TResult>
    {
        private IEnumerable<TSource> m_Source;
        private Func<TSource, TResult> m_Selector;

        private int m_ThreadId = Environment.CurrentManagedThreadId;

        private TResult m_Current = default!;
        private IEnumerator<TSource>? m_Enumerator;
        private int m_State = 0;

        public SelectManualEnumerable(IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            m_Source = source;
            m_Selector = selector;
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            //if(Interlocked.CompareExchange(ref m_State, 1,0) == 0)
            if (m_ThreadId == Environment.CurrentManagedThreadId && m_State == 0)
            {
                m_State = 1;
                return this;//new Enumerator(m_Source, m_Selector);
            }

            return new SelectManualEnumerable<TSource, TResult>(m_Source, m_Selector) { m_State = 1 };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        //private sealed class Enumerator : IEnumerator<TResult>
        //{
        //private IEnumerable<TSource> m_Source;
        //private Func<TSource, TResult> m_Selector;

        //private TResult m_Current = default!;
        //private IEnumerator<TSource> m_Enumerator;
        //private int m_State = 1;
        //public Enumerator(IEnumerable<TSource> source, Func<TSource, TResult> selector)
        //{
        //    m_Source = source;
        //    m_Selector = selector;
        //}

        public TResult Current => m_Current;

        object? IEnumerator.Current => Current;

        public void Dispose()
        {
            m_State = -1;
            m_Enumerator?.Dispose();
        }

        public bool MoveNext()
        {
            if (m_State == 1)
            {
                m_Enumerator = m_Source.GetEnumerator();
                m_State = 2;
            }

            if (m_State == 2)
            {
                Debug.Assert(m_Enumerator is not null);
                try
                {
                    while (m_Enumerator.MoveNext())
                    {
                        m_Current = m_Selector(m_Enumerator.Current);
                        return true;
                        //yield return
                    }
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            Dispose();
            return false;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }
        //}

    }
}
