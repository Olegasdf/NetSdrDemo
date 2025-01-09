
namespace NetSdrDemo
{
    /// <summary>
    /// Інтерфейс для NetSDR-клієнта (DI/Mocks)
    /// </summary>
    public interface INetSdrClient : IAsyncDisposable
    {
        bool IsConnected { get; }

        /// <summary>
        /// Асинхронне підключення по TCP до NetSDR-приймача
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Асинхронне відключення від NetSDR
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Команда [4.2.1] Receiver State (запуск/зупинка передачі IQ)
        /// </summary>
        /// <param name="start"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetReceiverStateAsync(bool start, CancellationToken cancellationToken = default);

        /// <summary>
        /// Команда [4.2.3] Receiver Frequency (зміна частоти)
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetReceiverFrequencyAsync(int frequency, CancellationToken cancellationToken = default);

        /// <summary>
        /// Метод приймає дані по UDP і пише у файл (IQ-дані)
        /// </summary>
        /// <param name="outputFile"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ReceiveIqDataAsync(string outputFile, CancellationToken cancellationToken = default);

        /// <summary>
        ///  Слухає «непрошені» (unsolicited) дані по TCP
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ListenForUnsolicitedItemsAsync(CancellationToken cancellationToken = default);
    }
}
