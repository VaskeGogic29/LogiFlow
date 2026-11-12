namespace LogiFlow.Domain.Entities.Common
{
    public interface IAuditable
    {
        DateTime CreatedAt { get; set; }   // UTC
        int? CreatedBy { get; set; }       // Id korisnika; null = seed/sistem
        DateTime? UpdatedAt { get; set; }  // null dok se zapis ne izmeni
        int? UpdatedBy { get; set; }
    }
}
