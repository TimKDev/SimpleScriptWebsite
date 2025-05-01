using System.Text;
using Docker.DotNet;
using SimpleScriptWebSite.Interfaces;

namespace SimpleScriptWebSite.Models;

public class ContainerSession
{
    private readonly IContainerRepository _containerRepository;
    private readonly MultiplexedStream _stream;
    private readonly CancellationTokenSource _cts;
    private readonly byte[] _buffer = new byte[81920];
    private readonly string _containerId;

    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? ErrorReceived;


    public ContainerSession(string containerId, MultiplexedStream stream, IContainerRepository containerRepository)
    {
        _containerId = containerId;
        _stream = stream;
        _containerRepository = containerRepository;
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

    public async ValueTask Cleanup()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
        _stream.Dispose();
        await StopContainerAsync();
    }

    private async Task StopContainerAsync()
    {
        await _containerRepository.StopContainerAsync(_containerId);
        await _containerRepository.RemoveContainerAsync(_containerId);
    }


    private async Task ReadOutputAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _stream.ReadOutputAsync(_buffer, 0, _buffer.Length, cancellationToken);
                if (result.EOF)
                {
                    break;
                }

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