using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Sistem
{
    // Pamti odgovor na zahtev sa zaglavljem Idempotency-Key, da ponovljen zahtev ne izvrsi radnju dvaput
    public class IdempotencyZapis
    {
        public int Id { get; private set; }
        public string Kljuc { get; private set; } = null!;         // vrednost zaglavlja Idempotency-Key (sa frontenda)
        public int KorisnikId { get; private set; }
        public string Putanja { get; private set; } = null!;       // npr. "POST /api/narudzbine"
        public string HashZahteva { get; private set; } = null!;   // SHA-256 tela zahteva
        public int? StatusKod { get; private set; }                // null = zahtev se jos obradjuje
        public string? TeloOdgovora { get; private set; }
        public DateTime KreiranU { get; private set; }
        public DateTime IsticeU { get; private set; }

        public bool JeZavrsen => StatusKod is not null;

        private IdempotencyZapis() { } // za EF Core

        public IdempotencyZapis(string kljuc, int korisnikId, string putanja, string hashZahteva,
                                DateTime sada, TimeSpan trajanje)
        {
            if (string.IsNullOrWhiteSpace(kljuc) || string.IsNullOrWhiteSpace(putanja)
                || string.IsNullOrWhiteSpace(hashZahteva))
                throw new DomainException("Ključ, putanja i hash zahteva su obavezni.");
            if (trajanje <= TimeSpan.Zero)
                throw new DomainException("Trajanje zapisa mora biti pozitivno.");

            Kljuc = kljuc.Trim();
            KorisnikId = korisnikId;
            Putanja = putanja.Trim();
            HashZahteva = hashZahteva;
            KreiranU = sada;
            IsticeU = sada + trajanje;
        }

        public bool JeIstekao(DateTime sada) => IsticeU <= sada;

        public bool OdgovaraZahtevu(string putanja, string hashZahteva) =>
            Putanja == putanja.Trim() && HashZahteva == hashZahteva;

        public void Zavrsi(int statusKod, string? teloOdgovora)
        {
            if (JeZavrsen)
                throw new DomainException("Odgovor za ovaj ključ je već zabeležen.");
            if (statusKod is < 100 or > 599)
                throw new DomainException("Neispravan HTTP status kod.");

            StatusKod = statusKod;
            TeloOdgovora = teloOdgovora;
        }
    }
}
