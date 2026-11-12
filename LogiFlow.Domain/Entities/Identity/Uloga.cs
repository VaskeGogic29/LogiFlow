using LogiFlow.Domain.Constants;
using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Identity
{
    public class Uloga : BaseEntity
    {

        public string Naziv { get; private set; } = null!;
        public string? Opis { get; private set; }


        private Uloga() { } // za EFCore

        public Uloga(string naziv, string? opis = null)
        {
            if (string.IsNullOrWhiteSpace(naziv) || !NaziviUloga.Sve.Contains(naziv))
                throw new DomainException($"Uloga '{naziv}' ne postoji u sistemu.");

            Naziv = naziv;
            Opis = opis?.Trim();
        }

        public void PromeniOpis(string? opis) => Opis = opis?.Trim();
    }
}
