using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Enums;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Catalog
{
    public class Proizvod : BaseEntity, ISoftDelete
    {
        private readonly List<IstorijaCene> _istorijaCena = [];

        public string Sifra { get; private set; } = null!;        // SKU, jedinstvena
        public string Naziv { get; private set; } = null!;
        public string? Opis { get; private set; }

        public int KategorijaId { get; private set; }
        public Kategorija Kategorija { get; private set; } = null!;

        public JedinicaMere JedinicaMere { get; private set; }
        public decimal TrenutnaCena { get; private set; }          // bez PDV-a
        public decimal PdvStopa { get; private set; }              // u procentima, npr. 20 ili 10

        public decimal? TezinaKg { get; private set; }
        public string? Dimenzije { get; private set; }             // npr. "30x20x10 cm"
        public string? SlikaUrl { get; private set; }
        public bool IsActive { get; private set; }

        public IReadOnlyCollection<IstorijaCene> IstorijaCena => _istorijaCena;

        private Proizvod() { } // za EF Core

        public Proizvod(string sifra, string naziv, Kategorija kategorija, JedinicaMere jedinicaMere,
                        decimal cena, decimal pdvStopa, DateTime sada, int? kreiraoKorisnikId,
                        string? opis = null, decimal? tezinaKg = null, string? dimenzije = null,
                        string? slikaUrl = null)
        {
            if (string.IsNullOrWhiteSpace(sifra))
                throw new DomainException("Šifra proizvoda je obavezna.");

            Sifra = sifra.Trim().ToUpperInvariant();
            IsActive = true;
            Izmeni(naziv, opis, kategorija, jedinicaMere, tezinaKg, dimenzije, slikaUrl);
            PromeniPdvStopu(pdvStopa);
            PromeniCenu(cena, sada, kreiraoKorisnikId);
        }

        public void Izmeni(string naziv, string? opis, Kategorija kategorija, JedinicaMere jedinicaMere,
                           decimal? tezinaKg, string? dimenzije, string? slikaUrl)
        {
            if (string.IsNullOrWhiteSpace(naziv))
                throw new DomainException("Naziv proizvoda je obavezan.");
            if (!kategorija.IsActive)
                throw new DomainException($"Kategorija '{kategorija.Naziv}' nije aktivna.");
            if (!Enum.IsDefined(jedinicaMere))
                throw new DomainException("Nepoznata jedinica mere.");
            if (tezinaKg is <= 0)
                throw new DomainException("Težina mora biti veća od nule.");

            Naziv = naziv.Trim();
            Opis = opis?.Trim();
            Kategorija = kategorija;
            KategorijaId = kategorija.Id;
            JedinicaMere = jedinicaMere;
            TezinaKg = tezinaKg;
            Dimenzije = dimenzije?.Trim();
            SlikaUrl = slikaUrl?.Trim();
        }

        public void PromeniCenu(decimal novaCena, DateTime sada, int? promenioKorisnikId)
        {
            if (novaCena <= 0)
                throw new DomainException("Cena mora biti veća od nule.");

            var vazeca = _istorijaCena.FirstOrDefault(i => i.VaziDo is null);
            if (vazeca is not null)
            {
                if (vazeca.Cena == novaCena)
                    return;
                vazeca.Zatvori(sada);
            }

            _istorijaCena.Add(new IstorijaCene(this, novaCena, sada, promenioKorisnikId));
            TrenutnaCena = novaCena;
        }

        public void PromeniPdvStopu(decimal pdvStopa)
        {
            if (pdvStopa < 0 || pdvStopa > 100)
                throw new DomainException("PDV stopa mora biti između 0 i 100.");
            PdvStopa = pdvStopa;
        }

        public void Deaktiviraj() => IsActive = false;
        public void Aktiviraj() => IsActive = true;
    }
}
