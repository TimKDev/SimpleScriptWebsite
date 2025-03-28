using Docker.DotNet;
using Docker.DotNet.Models;

namespace SimpleScriptWebSite.Controllers;

public class DockerDotNetRunner : IDockerDotNetRunner
{
    private readonly DockerClient _client;

    public DockerDotNetRunner()
    {
        //The host docker demon socket is added to the container using a volume in the docker compose
        _client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
    }

    public async Task<ContainerSession> RunDotNetDllAsync(
        string dllPath,
        string[]? args = null,
        Dictionary<string, string>? environmentVariables = null,
        int? memoryLimit = null,
        double? cpuLimit = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException($"Could not find the DLL at path: {dllPath}");
        }

        var dllDirectory = Path.GetDirectoryName(Path.GetFullPath(dllPath));
        var dllFileName = Path.GetFileName(dllPath);
        var files = Directory.GetFiles(dllDirectory);

        //Pull Image if not already loaded
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

        //Create Container
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
                        //Put Path into appsettings
                        $"/home/tim/Source/projects/SimpleScriptWebSite/ConsoleApp:/app"
                    }
                },
                Env = ConvertEnvironmentVariables(environmentVariables),
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

        // Attach to the container streams
        var stream = await _client.Containers.AttachContainerAsync(
            createResponse.ID,
            false,
            attachParameters,
            cancellationToken
        );

        // Start the container
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

    private IList<string> ConvertEnvironmentVariables(Dictionary<string, string>? envVars)
    {
        var result = new List<string>();
        if (envVars == null || envVars.Count == 0)
        {
            return result;
        }

        foreach (var kvp in envVars)
        {
            result.Add($"{kvp.Key}={kvp.Value}");
        }

        return result;
    }
}