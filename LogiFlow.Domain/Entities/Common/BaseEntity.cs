namespace LogiFlow.Domain.Entities.Common
{
    public abstract class BaseEntity : IAuditable
    {
        public int Id { get; protected set; }

        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
