using BenchmarkDotNet.Running;

namespace NetSdrDemo.Benchmarks
{
    public static class Benchmarks
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<NetSdrBenchmarks>();
        }
    }
}
