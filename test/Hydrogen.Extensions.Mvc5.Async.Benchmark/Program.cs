using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

namespace Hydrogen.Extensions.Mvc5.Async.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<AsyncControllerActionInvokerBenchmark>();
        }
    }

    class Config : ManualConfig
    {
        public Config()
        {
            //Add(new MemoryDiagnoser());
            //Add(new InliningDiagnoser());
        }
    }
}