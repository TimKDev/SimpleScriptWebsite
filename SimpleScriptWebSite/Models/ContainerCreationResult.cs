namespace SimpleScriptWebSite.Models;

public class ContainerCreationResult
{
    public ContainerCreationStatus Status { get; }
    public ContainerSession? Session { get; }
    public string? UserIdentifier { get; }

    private ContainerCreationResult(ContainerCreationStatus status, ContainerSession? session, string? userIdentifier)
    {
        Session = session;
        UserIdentifier = userIdentifier;
        Status = status;
    }

    public static ContainerCreationResult Create(ContainerSession session, string userIdentifier)
    {
        return new ContainerCreationResult(ContainerCreationStatus.Success, session, userIdentifier);
    }

    public static ContainerCreationResult Create(ContainerCreationStatus status)
    {
        return new ContainerCreationResult(status, null, null);
    }
}