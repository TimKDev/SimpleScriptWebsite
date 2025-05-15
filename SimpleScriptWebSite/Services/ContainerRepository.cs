using System.Security.Cryptography.X509Certificates;
using Docker.DotNet;
using Docker.DotNet.Models;
using Docker.DotNet.X509;
using Microsoft.Extensions.Options;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;
using System.IO.Compression;
using System.Text;
using System.Formats.Tar;

namespace SimpleScriptWebSite.Services;

public class ContainerRepository : IContainerRepository
{
    private readonly DockerClient _client;
    private const string ConsoleAppImageName = "console-app-container";

    public ContainerRepository()
    {
        var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST") ??
                         throw new Exception("Docker Host not set");

        var certPath = Environment.GetEnvironmentVariable("DOCKER_CERT_PATH") ??
                       throw new Exception("Docker Cert Path not set");

        var clientCertPath = Path.Combine(certPath, "cert.pem");
        var clientKeyPath = Path.Combine(certPath, "key.pem");

        var clientCert = X509Certificate2.CreateFromPemFile(clientCertPath, clientKeyPath);
        var credentials = new CertificateCredentials(clientCert);

        _client = new DockerClientConfiguration(new Uri(dockerHost), credentials).CreateClient();
    }


    public async Task<ContainerSession> CreateAndStartContainerAsync(
        string startCommand,
        List<string> binds,
        int? memoryLimitInMb = null,
        double? cpuLimitInPercent = null,
        CancellationToken cancellationToken = default)
    {
        await BuildConsoleAppImageAsync(cancellationToken);

        var cpuShares = cpuLimitInPercent.HasValue ? (long)(cpuLimitInPercent.Value * 1024) : 0;
        var memoryLimitBytes = memoryLimitInMb.HasValue ? (long)memoryLimitInMb.Value * 1024 * 1024 : 0;

        var createResponse = await _client.Containers.CreateContainerAsync(
            new CreateContainerParameters
            {
                Image = ConsoleAppImageName,
                AttachStdin = true,
                AttachStdout = true,
                AttachStderr = true,
                OpenStdin = true,
                HostConfig = new HostConfig
                {
                    Memory = memoryLimitBytes,
                    CPUShares = cpuShares,
                    Binds = binds,
                },
                Cmd = ["/bin/bash", "-c", startCommand]
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
            containerRepository: this,
            stream: stream
        );
    }

    public Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        return _client.Containers.RemoveContainerAsync(
            containerId,
            new ContainerRemoveParameters
            {
                Force = true
            },
            cancellationToken
        );
    }

    public Task StopContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        return _client.Containers.StopContainerAsync(
            containerId,
            new ContainerStopParameters
            {
                WaitBeforeKillSeconds = 10
            },
            cancellationToken
        );
    }

    private async Task BuildConsoleAppImageAsync(CancellationToken cancellationToken = default)
    {
        var images = await _client.Images.ListImagesAsync(new ImagesListParameters
        {
            All = true
        }, cancellationToken);

        if (images.Any(i => i.RepoTags?.Contains($"{ConsoleAppImageName}:latest") == true))
        {
            return;
        }

        var fullDockerfilePath = Path.Combine("/Dockerfile");

        var buildParameters = new ImageBuildParameters
        {
            Dockerfile = "Dockerfile",
            Tags = new List<string> { $"{ConsoleAppImageName}:latest" }
        };

        using var tarStream = new MemoryStream();
        await using (var archive = new TarWriter(tarStream, TarEntryFormat.Pax, leaveOpen: true))
        {
            await archive.WriteEntryAsync(fullDockerfilePath, "Dockerfile", cancellationToken);
        }

        tarStream.Seek(0, SeekOrigin.Begin);

        await _client.Images.BuildImageFromDockerfileAsync(
            buildParameters,
            tarStream,
            new List<AuthConfig>(),
            new Dictionary<string, string>(),
            new Progress<JSONMessage>(),
            cancellationToken
        );
    }
}