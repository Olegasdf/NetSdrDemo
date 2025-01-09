
namespace NetSdrDemo
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("NetSDR Demo App Start.");

            await using INetSdrClient client = new NetSdrClient();

            try
            {
                // Підключаємося
                await client.ConnectAsync();

                // Наприклад, встановити частоту
                await client.SetReceiverFrequencyAsync(100_000_000);

                // Запуск IQ
                await client.SetReceiverStateAsync(true);

                using var cts = new CancellationTokenSource();
                var receiveTask = client.ReceiveIqDataAsync("iq_dump.bin", cts.Token);

                await Task.Delay(TimeSpan.FromSeconds(5));

                // Зупинити прийом IQ
                cts.Cancel();
                await receiveTask;

                // Зупинка IQ
                await client.SetReceiverStateAsync(false);

                // Від'єднатися
                await client.DisconnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Main: {ex.Message}");
            }

            Console.WriteLine("NetSDR Demo App End.");
        }
    }
}