using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Transport
{
    public class Vozilo : BaseEntity, ISoftDelete
    {
        public string Registracija { get; private set; } = null!;
        public string Marka { get; private set; } = null!;
        public string Model { get; private set; } = null!;
        public decimal? NosivostKg { get; private set; }
        public bool IsActive { get; private set; }

        private Vozilo() { } // za EF Core

        public Vozilo(string registracija, string marka, string model, decimal? nosivostKg = null)
        {
            if (string.IsNullOrWhiteSpace(registracija))
                throw new DomainException("Registracija je obavezna.");

            Registracija = registracija.Trim().ToUpperInvariant();
            Izmeni(marka, model, nosivostKg);
            IsActive = true;
        }

        public void Izmeni(string marka, string model, decimal? nosivostKg)
        {
            if (string.IsNullOrWhiteSpace(marka) || string.IsNullOrWhiteSpace(model))
                throw new DomainException("Marka i model su obavezni.");
            if (nosivostKg is <= 0)
                throw new DomainException("Nosivost mora biti veća od nule.");

            Marka = marka.Trim();
            Model = model.Trim();
            NosivostKg = nosivostKg;
        }

        public void Deaktiviraj() => IsActive = false;
        public void Aktiviraj() => IsActive = true;
    }
}
