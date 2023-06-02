namespace Zengenti.Contensis.RequestHandler.Domain.ValueTypes;

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

    public DeploymentStatus Deployment { get; set; }
    public WorkflowStatus Workflow { get; set; }
}