using BenchmarkDotNet.Attributes;
using Moq;

namespace NetSdrDemo.Benchmarks
{
    /// <summary>
    /// Клас бенчмарків для INetSdrClient
    /// </summary>
    [MemoryDiagnoser]
    public class NetSdrBenchmarks
    {
        private INetSdrClient _client = null!;

        /// <summary>
        /// Налаштування
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            // Створюємо мок інтерфейсу INetSdrClient
            var mockClient = new Mock<INetSdrClient>();

            // Налаштовуємо стан "IsConnected = true"
            mockClient.SetupGet(c => c.IsConnected).Returns(true);

            // Мокаємо виклик SetReceiverFrequencyAsync
            // і просто повертаємо Task.CompletedTask
            mockClient
                .Setup(c => c.SetReceiverFrequencyAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Присвоюємо _client
            _client = mockClient.Object;
        }

        /// <summary>
        /// Вставлюємо частоту
        /// </summary>
        /// <returns></returns>
        [Benchmark]
        public async Task SetReceiverFrequencyBenchmark()
        {
            // Імітуємо виклик
            await _client.SetReceiverFrequencyAsync(123_456_789);
        }
    }
}
