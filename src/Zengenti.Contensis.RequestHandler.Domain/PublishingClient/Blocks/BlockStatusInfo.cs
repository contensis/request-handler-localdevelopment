namespace Zengenti.Contensis.RequestHandler.Domain.PublishingClient.Blocks;

public class BlockStatusInfo
{
    public BlockStatusInfo()
    {
    }

    public BlockStatusInfo(DeploymentStatus deployment, WorkflowStatus workflow)
    {
        Deployment = deployment;
        Workflow = workflow;
    }

    public DeploymentStatus Deployment { get; }
    public WorkflowStatus Workflow { get; }
}