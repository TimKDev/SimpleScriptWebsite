using SimpleScriptWebSite.Services;

namespace SimpleScriptWebSite.Models;

public class ContainerCreationResult
{
    public AddContainerStatus Status { get; }
    public UserSessionResources? Resource { get; }

    private ContainerCreationResult(AddContainerStatus status, UserSessionResources? resource)
    {
        Resource = resource;
        Status = status;
    }

    public static ContainerCreationResult Create(UserSessionResources session)
    {
        return new ContainerCreationResult(AddContainerStatus.Success, session);
    }

    public static ContainerCreationResult Create(AddContainerStatus status)
    {
        return new ContainerCreationResult(status, null);
    }
}