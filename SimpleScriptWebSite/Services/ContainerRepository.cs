using System.Security.Cryptography.X509Certificates;
using Docker.DotNet;
using Docker.DotNet.Models;
using Docker.DotNet.X509;
using SimpleScriptWebSite.Interfaces;
using SimpleScriptWebSite.Models;

namespace SimpleScriptWebSite.Services;

public class ContainerRepository : IContainerRepository
{
    private readonly DockerClient _client;

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
        string imageName,
        string startCommand,
        List<string> binds,
        int? memoryLimit = null,
        double? cpuLimit = null,
        CancellationToken cancellationToken = default)
    {
        await _client.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = imageName,
            },
            null,
            new Progress<JSONMessage>(),
            cancellationToken
        );

        var cpuShares = cpuLimit.HasValue ? (long)(cpuLimit.Value * 1024) : 0;
        var memoryLimitBytes = memoryLimit.HasValue ? (long)memoryLimit.Value * 1024 * 1024 : 0;

        var createResponse = await _client.Containers.CreateContainerAsync(
            new CreateContainerParameters
            {
                Image = imageName,
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
}