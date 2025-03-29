using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace SimpleScriptWebSite.Controllers;

public class ContainerSession : IDisposable
{
    private readonly DockerClient _client;
    private readonly MultiplexedStream _stream;
    private CancellationTokenSource _cts;
    private readonly byte[] _buffer = new byte[81920];
    private bool _disposedValue;

    public string ContainerId { get; }
    public event EventHandler<string> OutputReceived;
    public event EventHandler<string> ErrorReceived;

    public ContainerSession(string containerId, DockerClient client, MultiplexedStream stream)
    {
        ContainerId = containerId;
        _client = client;
        _stream = stream;
        _cts = new CancellationTokenSource();

        // Start reading stream outputs
        Task.Run(() => ReadOutputAsync(_cts.Token));
    }


    public async Task SendInputAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        if (!input.EndsWith('\n'))
        {
            input += "\n";
        }

        var bytes = Encoding.UTF8.GetBytes(input);
        await _stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
    }

    public async Task StopContainerAsync()
    {
        try
        {
            await _client.Containers.StopContainerAsync(
                ContainerId,
                new ContainerStopParameters
                {
                    WaitBeforeKillSeconds = 10
                }
            );

            await _client.Containers.RemoveContainerAsync(
                ContainerId,
                new ContainerRemoveParameters
                {
                    Force = true
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping container: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
                _stream?.Dispose();
            }

            _disposedValue = true;
        }
    }

    private async Task ReadOutputAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _stream.ReadOutputAsync(_buffer, 0, _buffer.Length, cancellationToken);
                if (result.EOF)
                    break;

                var message = Encoding.UTF8.GetString(_buffer, 0, result.Count);

                if (result.Target == MultiplexedStream.TargetStream.StandardOut)
                {
                    OutputReceived?.Invoke(this, message);
                }
                else if (result.Target == MultiplexedStream.TargetStream.StandardError)
                {
                    ErrorReceived?.Invoke(this, message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke(this, $"Stream reading error: {ex.Message}");
        }
    }
}