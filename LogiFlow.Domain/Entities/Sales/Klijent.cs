using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Entities.Identity;
using LogiFlow.Domain.Enums;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Sales
{
    public class Klijent : BaseEntity
    {
        private readonly List<Adresa> _adrese = [];

        public int KorisnikId { get; private set; }
        public Korisnik Korisnik { get; private set; } = null!;

        public TipKlijenta TipKlijenta { get; private set; }
        public string Naziv { get; private set; } = null!;      // firma ili ime i prezime
        public string? Pib { get; private set; }
        public string? MaticniBroj { get; private set; }
        public string? KontaktTelefon { get; private set; }
        public string? Napomena { get; private set; }

        public IReadOnlyCollection<Adresa> Adrese => _adrese;

        private Klijent() { } // za EF Core

        public Klijent(int korisnikId, TipKlijenta tip, string naziv, string? pib,
                       string? maticniBroj, string? kontaktTelefon, string? napomena = null)
        {
            KorisnikId = korisnikId;
            IzmeniPodatke(tip, naziv, pib, maticniBroj, kontaktTelefon, napomena);
        }

        public void IzmeniPodatke(TipKlijenta tip, string naziv, string? pib,
                                  string? maticniBroj, string? kontaktTelefon, string? napomena)
        {
            if (string.IsNullOrWhiteSpace(naziv))
                throw new DomainException("Naziv klijenta je obavezan.");

            pib = pib?.Trim();
            maticniBroj = maticniBroj?.Trim();

            if (tip == TipKlijenta.PravnoLice)
            {
                if (!JeBrojDuzine(pib, 9))
                    throw new DomainException("PIB pravnog lica mora imati tačno 9 cifara.");
                if (!JeBrojDuzine(maticniBroj, 8))
                    throw new DomainException("Matični broj pravnog lica mora imati tačno 8 cifara.");
            }
            else
            {
                pib = null;
                maticniBroj = null;
            }

            TipKlijenta = tip;
            Naziv = naziv.Trim();
            Pib = pib;
            MaticniBroj = maticniBroj;
            KontaktTelefon = kontaktTelefon?.Trim();
            Napomena = napomena?.Trim();
        }

        public Adresa DodajAdresu(string? naziv, string ulica, string broj, string grad,
                                  string postanskiBroj, string? napomena = null)
        {
            var adresa = new Adresa(this, naziv, ulica, broj, grad, postanskiBroj, napomena);

            if (!_adrese.Any(a => a.IsActive))
                adresa.PostaviPodrazumevanu(true);   // prva adresa je automatski podrazumevana

            _adrese.Add(adresa);
            return adresa;
        }

        public void IzmeniAdresu(int adresaId, string? naziv, string ulica, string broj,
                                 string grad, string postanskiBroj, string? napomena) =>
            NadjiAktivnuAdresu(adresaId).Izmeni(naziv, ulica, broj, grad, postanskiBroj, napomena);

        public void PostaviPodrazumevanuAdresu(int adresaId)
        {
            var nova = NadjiAktivnuAdresu(adresaId);
            foreach (var a in _adrese)
                a.PostaviPodrazumevanu(false);
            nova.PostaviPodrazumevanu(true);
        }

        public void DeaktivirajAdresu(int adresaId)
        {
            var adresa = NadjiAktivnuAdresu(adresaId);
            var bilaPodrazumevana = adresa.IsPodrazumevana;
            adresa.Deaktiviraj();

            if (bilaPodrazumevana)
                _adrese.FirstOrDefault(a => a.IsActive)?.PostaviPodrazumevanu(true);
        }

        // Zaštita od IDOR-a: adresa mora pripadati baš ovom klijentu i biti aktivna.
        public Adresa NadjiAktivnuAdresu(int adresaId) =>
            _adrese.FirstOrDefault(a => a.Id == adresaId && a.IsActive)
            ?? throw new DomainException("Adresa ne postoji ili ne pripada klijentu.");

        private static bool JeBrojDuzine(string? vrednost, int duzina) =>
            vrednost is not null && vrednost.Length == duzina && vrednost.All(char.IsDigit);
    }
}
