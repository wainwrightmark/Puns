using System;
using BenchmarkDotNet.Running;
using Puns;

namespace Benchmark
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<PunStrategyBenchmark>();
        }
    }
}
