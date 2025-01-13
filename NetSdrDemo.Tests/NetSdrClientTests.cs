using Moq;

namespace NetSdrDemo.Tests
{
    /// <summary>
    /// Unit-тести
    /// </summary>
    public class NetSdrClientTests
    {
        /// <summary>
        /// Підключення до NetSdrClient
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ConnectAsync_WhenAlreadyConnected_ThrowsInvalidOperation()
        {
            INetSdrClient client = new NetSdrClient();
            await client.ConnectAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await client.ConnectAsync();
            });
        }

        /// <summary>
        /// Відключення від NetSdrClient
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task DisconnectAsync_WhenNotConnected_DoesNotThrow()
        {

            INetSdrClient client = new NetSdrClient();

            await client.DisconnectAsync();
        }

        /// <summary>
        /// запуск/зупинка передачі IQ
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task SetReceiverStateAsync_WithoutConnect_ThrowsInvalidOperation()
        {
            INetSdrClient client = new NetSdrClient();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.SetReceiverStateAsync(true));
        }

        /// <summary>
        /// Команда [4.2.3] Receiver Frequency (зміна частоти)
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task SetReceiverFrequencyAsync_WithoutConnect_ThrowsInvalidOperation()
        {
            INetSdrClient client = new NetSdrClient();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.SetReceiverFrequencyAsync(100000000));
        }

        /// <summary>
        /// Мок тест
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task MockedINetSdrClient_UsageExample()
        {
            var mock = new Mock<INetSdrClient>();

            // налаштування мок, щоб IsConnected завжди повертав true
            mock.SetupGet(m => m.IsConnected).Returns(true);

            // налаштування мок для SetReceiverFrequencyAsync
            mock.Setup(m => m.SetReceiverFrequencyAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // отримуємо fake клієнта
            var client = mock.Object;

            // вставновлюємо частоту на мок інтерфейсі
            await client.SetReceiverFrequencyAsync(100000000);

            // перевіряємо, чи метод SetReceiverFrequencyAsync виконався 1 раз із вказаною частотою і будь яким CancellationToken
            mock.Verify(m => m.SetReceiverFrequencyAsync(100000000, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}