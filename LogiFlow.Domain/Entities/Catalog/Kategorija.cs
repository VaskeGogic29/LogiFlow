using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Catalog
{
    public class Kategorija : BaseEntity, ISoftDelete
    {
        public string Naziv { get; private set; } = null!;
        public string? Opis { get; private set; }

        public int? NadkategorijaId { get; private set; }
        public Kategorija? Nadkategorija { get; private set; }

        public bool IsActive { get; private set; }

        private Kategorija() { } // za EF Core

        public Kategorija(string naziv, string? opis = null, Kategorija? nadkategorija = null)
        {
            Izmeni(naziv, opis);
            PromeniNadkategoriju(nadkategorija);
            IsActive = true;
        }

        public void Izmeni(string naziv, string? opis)
        {
            if (string.IsNullOrWhiteSpace(naziv))
                throw new DomainException("Naziv kategorije je obavezan.");
            Naziv = naziv.Trim();
            Opis = opis?.Trim();
        }

        public void PromeniNadkategoriju(Kategorija? nadkategorija)
        {
            if (nadkategorija is not null)
            {
                if (nadkategorija == this || (Id != 0 && nadkategorija.Id == Id))
                    throw new DomainException("Kategorija ne može biti sama sebi nadkategorija.");
                if (!nadkategorija.IsActive)
                    throw new DomainException($"Nadkategorija '{nadkategorija.Naziv}' nije aktivna.");
            }

            Nadkategorija = nadkategorija;
            NadkategorijaId = nadkategorija?.Id;
        }

        public void Deaktiviraj() => IsActive = false;
        public void Aktiviraj() => IsActive = true;
    }
}
