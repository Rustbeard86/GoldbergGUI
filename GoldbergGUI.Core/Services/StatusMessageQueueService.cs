using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace GoldbergGUI.Core.Services;

/// <summary>
/// Service for managing status messages with queue and automatic display timing
/// </summary>
public interface IStatusMessageQueue
{
    /// <summary>
    /// Adds a status message to the queue
    /// </summary>
    void Enqueue(string message, TimeSpan? displayDuration = null);
    
    /// <summary>
    /// Gets the current status message to display
    /// </summary>
    string CurrentMessage { get; }
    
    /// <summary>
    /// Event raised when the current message changes
    /// </summary>
    event EventHandler<string>? MessageChanged;
    
    /// <summary>
    /// Starts processing the message queue
    /// </summary>
    void Start();
    
    /// <summary>
    /// Stops processing the message queue
    /// </summary>
    void Stop();
}

public sealed class StatusMessageQueueService(ILogger<StatusMessageQueueService> log) : IStatusMessageQueue, IDisposable
{
    private readonly ConcurrentQueue<StatusMessage> _messageQueue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _semaphore = new(0);
    private Task? _processingTask;
    private string _currentMessage = "Ready.";
    private const int DefaultDisplayDurationMs = 3000;

    public string CurrentMessage
    {
        get => _currentMessage;
        private set
        {
            if (_currentMessage == value) return;
            _currentMessage = value;
            MessageChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<string>? MessageChanged;

    public void Enqueue(string message, TimeSpan? displayDuration = null)
    {
        var duration = displayDuration ?? TimeSpan.FromMilliseconds(DefaultDisplayDurationMs);
        _messageQueue.Enqueue(new StatusMessage(message, duration));
        _semaphore.Release();
        log.LogDebug("Status message queued: {Message}", message);
    }

    public void Start()
    {
        if (_processingTask != null) return;

        _processingTask = Task.Run(async () => await ProcessQueueAsync().ConfigureAwait(false));
        log.LogInformation("Status message queue started");
    }

    public void Stop()
    {
        _cts.Cancel();
        _semaphore.Release(); // Unblock if waiting
        _processingTask?.Wait(TimeSpan.FromSeconds(2));
        log.LogInformation("Status message queue stopped");
    }

    private async Task ProcessQueueAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                // Wait for a message to be available
                await _semaphore.WaitAsync(_cts.Token).ConfigureAwait(false);

                if (_messageQueue.TryDequeue(out var statusMessage))
                {
                    CurrentMessage = statusMessage.Message;
                    log.LogDebug("Displaying status: {Message} for {Duration}ms", 
                        statusMessage.Message, statusMessage.DisplayDuration.TotalMilliseconds);

                    // Display the message for the specified duration
                    await Task.Delay(statusMessage.DisplayDuration, _cts.Token).ConfigureAwait(false);

                    // If queue is empty, set back to "Ready."
                    if (_messageQueue.IsEmpty)
                    {
                        CurrentMessage = "Ready.";
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error processing status message queue");
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _cts.Dispose();
        _semaphore.Dispose();
    }

    private sealed record StatusMessage(string Message, TimeSpan DisplayDuration);
}
