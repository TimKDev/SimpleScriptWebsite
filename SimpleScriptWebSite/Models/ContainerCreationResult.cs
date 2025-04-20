namespace SimpleScriptWebSite.Models;

public class ContainerCreationResult
{
    public ContainerCreationStatus Status { get; set; }
    public ContainerSession? Session { get; set; }

    private ContainerCreationResult(ContainerCreationStatus status, ContainerSession? session)
    {
        Session = session;
        Status = status;
    }

    public static ContainerCreationResult Create(ContainerSession session)
    {
        return new ContainerCreationResult(ContainerCreationStatus.Success, session);
    }

    public static ContainerCreationResult Create(ContainerCreationStatus status)
    {
        return new ContainerCreationResult(status, null);
    }
}