namespace LogiFlow.Domain.Entities.Common
{
    public interface ISoftDelete
    {
        bool IsActive { get; }
    }
}
