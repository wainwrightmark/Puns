using System;
using BenchmarkDotNet.Running;
using Puns;

namespace Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<PunStrategyBenchmark>();
        }
    }
}
