using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace SimpleScriptWebSite.Services;

public class ContainerSession : IDisposable
{
    private readonly DockerClient _client;
    private readonly MultiplexedStream _stream;
    private readonly CancellationTokenSource _cts;
    private readonly byte[] _buffer = new byte[81920];
    private readonly string _containerId;

    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? ErrorReceived;

    public ContainerSession(string containerId, DockerClient client, MultiplexedStream stream)
    {
        _containerId = containerId;
        _client = client;
        _stream = stream;
        _cts = new CancellationTokenSource();
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

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _stream.Dispose();
        _ = StopContainerAsync();
    }

    private async Task StopContainerAsync()
    {
        await _client.Containers.StopContainerAsync(
            _containerId,
            new ContainerStopParameters
            {
                WaitBeforeKillSeconds = 10
            }
        );

        await _client.Containers.RemoveContainerAsync(
            _containerId,
            new ContainerRemoveParameters
            {
                Force = true
            }
        );
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
        catch (OperationCanceledException _)
        {
            //Log Normal Shutdown
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke(this, $"Stream reading error: {ex.Message}");
        }
    }
}