using Docker.DotNet;
using Docker.DotNet.Models;
using SimpleScriptWebSite.Interfaces;

namespace SimpleScriptWebSite.Services;

internal class DockerDotNetRunner : IDockerDotNetRunner
{
    private readonly DockerClient _client;
    private readonly ILogger<DockerDotNetRunner> _logger;

    public DockerDotNetRunner(ILogger<DockerDotNetRunner> logger)
    {
        _logger = logger;
        var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST") ??
                         throw new Exception("Docker Host not set");

        _logger.LogCritical($"Docker Host: {dockerHost}");

        _client = new DockerClientConfiguration(new Uri(dockerHost)).CreateClient();
    }

    public async Task<ContainerSession> RunDotNetDllAsync(
        string dllFileName,
        string[]? args = null,
        int? memoryLimit = null,
        double? cpuLimit = null,
        CancellationToken cancellationToken = default)
    {
        await _client.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = "mcr.microsoft.com/dotnet/runtime",
                Tag = "9.0"
            },
            null,
            new Progress<JSONMessage>(),
            cancellationToken
        );

        var cpuShares = cpuLimit.HasValue ? (long)(cpuLimit.Value * 1024) : 0;
        var memoryLimitBytes = memoryLimit.HasValue ? (long)memoryLimit.Value * 1024 * 1024 : 0;

        var containerArgs = $"dotnet /app/{dllFileName}";
        if (args != null && args.Length > 0)
        {
            containerArgs += " " + string.Join(" ", args);
        }

        var createResponse = await _client.Containers.CreateContainerAsync(
            new CreateContainerParameters
            {
                Image = "mcr.microsoft.com/dotnet/runtime:9.0",
                AttachStdin = true,
                AttachStdout = true,
                AttachStderr = true,
                Tty = false,
                OpenStdin = true,
                StdinOnce = false,
                HostConfig = new HostConfig
                {
                    Memory = memoryLimitBytes,
                    CPUShares = cpuShares,
                    Binds = new List<string>
                    {
                        $"/ConsoleApp:/app"
                    }
                },
                Cmd = new List<string> { "/bin/bash", "-c", containerArgs }
            },
            cancellationToken
        );

        var attachParameters = new ContainerAttachParameters
        {
            Stream = true,
            Stdin = true,
            Stdout = true,
            Stderr = true
        };

        var stream = await _client.Containers.AttachContainerAsync(
            createResponse.ID,
            false,
            attachParameters,
            cancellationToken
        );

        await _client.Containers.StartContainerAsync(
            createResponse.ID,
            new ContainerStartParameters(),
            cancellationToken
        );

        return new ContainerSession(
            containerId: createResponse.ID,
            client: _client,
            stream: stream
        );
    }
}