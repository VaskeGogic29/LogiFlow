using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Identity
{
    public class Ovlascenje : BaseEntity
    {
        public string Kod { get; private set; } = null!;
        public string? Opis { get; private set; }
        public string Grupa { get; private set; } = null!;

        private Ovlascenje() { } // za EF Core

        public Ovlascenje(string kod, string grupa, string? opis = null)
        {
            if (string.IsNullOrWhiteSpace(kod))
                throw new DomainException("Kod ovlašćenja je obavezan.");
            if (string.IsNullOrWhiteSpace(grupa))
                throw new DomainException("Grupa ovlašćenja je obavezna.");

            Kod = kod.Trim().ToLowerInvariant();
            Grupa = grupa.Trim();
            Opis = opis?.Trim();
        }

        public void PromeniOpis(string? opis) => Opis = opis?.Trim();
    }
}
