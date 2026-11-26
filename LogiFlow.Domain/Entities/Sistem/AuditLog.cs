using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Sistem
{
    //append-only, zato ne nasledjuje Base, bezbednosni zapis (dnevnik)
    public class AuditLog
    {
        public int Id { get; private set; }
        public int? KorisnikId { get; private set; }         // null = neprijavljen (npr neuspesna prijava)
        public string Akcija { get; private set; } = null!;  // vrednost iz AkcijeAudita
        public string? Entitet { get; private set; }         // npr Faktura
        public int? EntitetId { get; private set; }
        public string? StaraVrednost { get; private set; }   // JSON, pravi ga servis
        public string? NovaVrednost { get; private set; }    // JSON
        public DateTime Vreme { get; private set; }
        public string? IpAdresa { get; private set; }

        private AuditLog() { } // za EF Core

        public static AuditLog Zabelezi(string akcija, DateTime sada, int? korisnikId = null,
                                        string? entitet = null, int? entitetId = null,
                                        string? staraVrednost = null, string? novaVrednost = null,
                                        string? ipAdresa = null)
        {
            if (string.IsNullOrWhiteSpace(akcija))
                throw new DomainException("Akcija je obavezna.");
            if (entitetId is not null && string.IsNullOrWhiteSpace(entitet))
                throw new DomainException("Uz EntitetId mora biti naveden i entitet.");

            return new AuditLog
            {
                Akcija = akcija.Trim(),
                Vreme = sada,
                KorisnikId = korisnikId,
                Entitet = entitet?.Trim(),
                EntitetId = entitetId,
                StaraVrednost = staraVrednost,
                NovaVrednost = novaVrednost,
                IpAdresa = ipAdresa?.Trim()
            };
        }
    }
}
