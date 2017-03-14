using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

namespace MvcAsync.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<AsyncControllerBenchmark>();
            BenchmarkRunner.Run<AsyncControllerActionInvokerBenchmark>();
        }
    }

    class Config : ManualConfig
    {
        public Config()
        {
            Add(new MemoryDiagnoser());
            //Add(new InliningDiagnoser());
        }
    }
}