using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Transport
{
    public class Zona : BaseEntity, ISoftDelete
    {
        public string Naziv { get; private set; } = null!;
        public string? Opis { get; private set; }
        public bool IsActive { get; private set; }

        private Zona() { } // za EF Core

        public Zona(string naziv, string? opis = null)
        {
            Izmeni(naziv, opis);
            IsActive = true;
        }

        public void Izmeni(string naziv, string? opis)
        {
            if (string.IsNullOrWhiteSpace(naziv))
                throw new DomainException("Naziv zone je obavezan.");
            Naziv = naziv.Trim();
            Opis = opis?.Trim();
        }

        public void Deaktiviraj() => IsActive = false;
        public void Aktiviraj() => IsActive = true;
    }
}
