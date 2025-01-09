using System.Net.Sockets;

namespace NetSdrDemo
{
    /// <summary>
    /// Реалізація NetSDR-клієнта
    /// </summary>
    public class NetSdrClient : INetSdrClient
    {
        // Порти за замовчуванням згідно з описом протоколу (TCP=50000, UDP=60000).
        public const int DefaultTcpPort = 50000;
        public const int DefaultUdpPort = 60000;

        // Ідентифікатори команд протоколу:
        // [4.2.1] Receiver State (запуск/зупинка IQ)
        // [4.2.3] Receiver Frequency (зміна частоти)
        private const int ReceiverStateCommand = 1;
        private const int ReceiverFrequencyCommand = 2;

        // IP-адреса пристрою, до якого під’єднуємося
        private readonly string _ipAddress;

        // TCP-клієнт/стрім для надсилання команд,
        // UDP-клієнт для отримання IQ-даних.
        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private UdpClient? _udpClient;

        // Прапорець, чи є активне з'єднання з приймачем.
        private bool _isConnected;

        /// <summary>
        /// Властивість для перевірки підключення
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="ipAddress"></param>
        public NetSdrClient(string ipAddress = "127.0.0.1")
        {
            _ipAddress = ipAddress;
        }

        /// <summary>
        /// Асинхронне підключення по TCP до NetSDR-приймача
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            // Перевіряємо, чи не підключені вже
            if (_isConnected)
                throw new InvalidOperationException("Already connected.");

            try
            {
                // Створюємо TcpClient і підключаємось за вказаною IP-адресою та портом
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(_ipAddress, DefaultTcpPort, cancellationToken);

                // Отримуємо NetworkStream для надсилання/отримання TCP-даних
                _networkStream = _tcpClient.GetStream();

                // Ініціалізуємо UdpClient, щоб приймати IQ-дані на порту 60000
                _udpClient = new UdpClient(DefaultUdpPort);

                _isConnected = true;
                Console.WriteLine("Connected to NetSDR.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connect failed: {ex.Message}");
                // У разі помилки намагаємося коректно відключитися, щоб не висіли відкриті сокети
                await DisconnectAsync();
                throw;
            }
        }

        /// <summary>
        /// Асинхронне відключення від NetSDR
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            // Якщо немає підключення, просто виводимо повідомлення й повертаємось
            if (!_isConnected)
            {
                Console.WriteLine("No active connection to disconnect.");
                return;
            }

            try
            {
                // Закриваємо TCP-стрім, TCP-клієнт і UDP-клієнт
                _networkStream?.Close();
                _tcpClient?.Close();
                _udpClient?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Disconnect failed: {ex.Message}");
            }
            finally
            {
                // У будь-якому разі позначаємо, що з’єднання більше немає
                _isConnected = false;
                // await Task.Yield() — дає можливість асинхронному середовищу плануватися
                await Task.Yield();
                Console.WriteLine("Disconnected from NetSDR.");
            }
        }

        /// <summary>
        /// Команда [4.2.1] Receiver State (запуск/зупинка передачі IQ)
        /// </summary>
        /// <param name="start"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SetReceiverStateAsync(bool start, CancellationToken cancellationToken = default)
        {
            EnsureConnected();

            // Формуємо тіло команди: 1 = Start, 0 = Stop
            var cmdData = new[] { (byte)(start ? 1 : 0) };
            await SendCommandAsync(ReceiverStateCommand, cmdData, cancellationToken);

            // Чекаємо на відповідь ACK/NAK
            await ReceiveAckNakAsync(cancellationToken);
        }

        /// <summary>
        /// Команда [4.2.3] Receiver Frequency (зміна частоти)
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SetReceiverFrequencyAsync(int frequency, CancellationToken cancellationToken = default)
        {
            EnsureConnected();

            // Переводимо int frequency у байти (4 байти, Little Endian)
            var cmdData = BitConverter.GetBytes(frequency);
            await SendCommandAsync(ReceiverFrequencyCommand, cmdData, cancellationToken);

            // Знову чекаємо на ACK/NAK
            await ReceiveAckNakAsync(cancellationToken);
        }

        /// <summary>
        /// Метод приймає дані по UDP і пише у файл (IQ-дані)
        /// </summary>
        /// <param name="outputFile"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task ReceiveIqDataAsync(string outputFile, CancellationToken cancellationToken = default)
        {
            EnsureConnected();
            if (_udpClient == null)
                throw new InvalidOperationException("UDP client not initialized.");

            // Відкриваємо файл у асинхронному режимі (useAsync: true),
            // щоб мати можливість asynchrony з FileStream.
            await using var fs = new FileStream(
                outputFile,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                useAsync: true);

            Console.WriteLine("Start receiving IQ data via UDP... (stop via cancellation token)");

            // Безкінечний цикл, що виходить при скасуванні (cancellationToken)
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Читаємо пакет UDP (приблизно 4.5.1 NetSDR Output Data Item)
                    UdpReceiveResult res = await _udpClient.ReceiveAsync(cancellationToken);

                    // Записуємо отримані байти у файл
                    byte[] buffer = res.Buffer;
                    await fs.WriteAsync(buffer, 0, buffer.Length, cancellationToken);

                    Console.WriteLine($"Received {buffer.Length} bytes of IQ data.");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("ReceiveIqDataAsync canceled.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"UDP receive error: {ex.Message}");
                }
            }

            Console.WriteLine("Stop receiving IQ data.");
        }

        /// <summary>
        /// Слухає «непрошені» (unsolicited) дані по TCP
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task ListenForUnsolicitedItemsAsync(CancellationToken cancellationToken = default)
        {
            EnsureConnected();
            if (_networkStream == null)
                throw new InvalidOperationException("NetworkStream not initialized.");

            var buffer = new byte[256];
            Console.WriteLine("Listening for unsolicited items... (stop via cancellation token)");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Читаємо з TCP-потоку
                    int readBytes = await _networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (readBytes == 0)
                    {
                        Console.WriteLine("Stream closed by remote side.");
                        break;
                    }
                    // У реальній імплементації - парсили б "unsolicited control item"
                    Console.WriteLine($"Unsolicited data: {BitConverter.ToString(buffer, 0, readBytes)}");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("ListenForUnsolicitedItemsAsync canceled.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading unsolicited data: {ex.Message}");
                    // Можливо break чи повторювати
                }
            }
        }

        /// <summary>
        /// Надіслати команду: [CommandByte + Data...]
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="data"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task SendCommandAsync(int commandType, byte[] data, CancellationToken token)
        {
            EnsureConnected();
            if (_networkStream == null)
                throw new InvalidOperationException("No network stream for sending.");

            // Пакет: перший байт = тип команди, далі – дані
            var packet = new byte[data.Length + 1];
            packet[0] = (byte)commandType;
            Array.Copy(data, 0, packet, 1, data.Length);

            // Асинхронно пишемо в TCP
            await _networkStream.WriteAsync(packet, 0, packet.Length, token);
            Console.WriteLine($"Command {commandType} sent.");
        }

        /// <summary>
        /// Приватний метод для зчитування 1 байта ACK/NAK (1=ACK, 0=NAK)
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task ReceiveAckNakAsync(CancellationToken token)
        {
            EnsureConnected();
            if (_networkStream == null)
                throw new InvalidOperationException("No network stream for reading.");

            var response = new byte[1];
            int bytesRead = await _networkStream.ReadAsync(response, 0, 1, token);

            if (bytesRead == 0)
            {
                Console.WriteLine("No ACK/NAK received.");
                return;
            }

            // Реакція на отримані значення
            switch (response[0])
            {
                case 1:
                    Console.WriteLine("ACK received. Command successful.");
                    break;
                case 0:
                    Console.WriteLine("NAK received. Command failed!");
                    break;
                default:
                    Console.WriteLine($"Unknown ACK/NAK byte: {response[0]}");
                    break;
            }
        }

        /// <summary>
        /// Перевірка, що ми реально підключені. Якщо ні — виняток.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void EnsureConnected()
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to NetSDR.");
            }
        }

        /// <summary>
        /// Реалізація IAsyncDisposable (щоб using var client = new NetSdrClient(); працювало асинхронно)
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();
            GC.SuppressFinalize(this);
        }
    }
}
