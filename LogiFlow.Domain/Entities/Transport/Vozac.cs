using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Entities.Identity;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Transport
{
    public class Vozac : BaseEntity
    {
        private readonly List<ZaduzenjeVozila> _zaduzenja = [];

        public int KorisnikId { get; private set; }
        public Korisnik Korisnik { get; private set; } = null!;

        public string BrojVozacke { get; private set; } = null!;
        public string KategorijaVozacke { get; private set; } = null!;

        public int ZonaId { get; private set; }
        public Zona Zona { get; private set; } = null!;

        public bool IsDostupan { get; private set; }

        public IReadOnlyCollection<ZaduzenjeVozila> Zaduzenja => _zaduzenja;

        private Vozac() { } // za EF Core read

        public Vozac(int korisnikId, int zonaId, string brojVozacke, string kategorijaVozacke)
        {
            KorisnikId = korisnikId;
            ZonaId = zonaId;
            IzmeniVozacku(brojVozacke, kategorijaVozacke);
            IsDostupan = true;
        }

        public void IzmeniVozacku(string brojVozacke, string kategorijaVozacke)
        {
            if (string.IsNullOrWhiteSpace(brojVozacke) || string.IsNullOrWhiteSpace(kategorijaVozacke))
                throw new DomainException("Broj i kategorija vozačke su obavezni.");
            BrojVozacke = brojVozacke.Trim();
            KategorijaVozacke = kategorijaVozacke.Trim().ToUpperInvariant();
        }

        public void PromeniZonu(int novaZonaId) => ZonaId = novaZonaId;

        public void PostaviDostupnost(bool dostupan) => IsDostupan = dostupan;

        public ZaduzenjeVozila? AktivnoZaduzenje() => _zaduzenja.FirstOrDefault(z => z.JeAktivno);

        public void ZaduziVozilo(Vozilo vozilo, int zaduzioKorisnikId, DateTime sada, string? napomena = null)
        {
            if (!vozilo.IsActive)
                throw new DomainException($"Vozilo {vozilo.Registracija} nije aktivno.");

            var aktivno = AktivnoZaduzenje();
            if (aktivno is not null)
            {
                if (aktivno.Vozilo == vozilo || (vozilo.Id != 0 && aktivno.VoziloId == vozilo.Id))
                    throw new DomainException($"Vozač već duži vozilo {vozilo.Registracija}.");

                aktivno.Razduzi(sada);   // promena vozila: staro se razduzuje, istorija ostaje
            }

            _zaduzenja.Add(new ZaduzenjeVozila(this, vozilo, zaduzioKorisnikId, sada, napomena));
        }

        public void RazduziVozilo(DateTime sada)
        {
            var aktivno = AktivnoZaduzenje()
                ?? throw new DomainException("Vozač trenutno ne duži nijedno vozilo.");
            aktivno.Razduzi(sada);
        }
    }
}
