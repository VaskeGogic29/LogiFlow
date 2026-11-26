using LogiFlow.Domain.Enums;
using LogiFlow.Domain.Exceptions;

namespace LogiFlow.Domain.Entities.Sistem
{
    //append-only, zato ne nasledjuje Base
    public class IstorijaStatusa
    {
        public int Id { get; private set; }
        public TipEntitetaIstorije TipEntiteta { get; private set; }
        public int EntitetId { get; private set; }
        public string? StariStatus { get; private set; }     // null = prvi status (nastanak)
        public string NoviStatus { get; private set; } = null!;
        public DateTime Vreme { get; private set; }
        public int? KorisnikId { get; private set; }         // null = promenu je uradio sistem
        public string? Napomena { get; private set; }

        private IstorijaStatusa() { } // za EF Core

        public static IstorijaStatusa Zabelezi<TStatus>(
            TipEntitetaIstorije tipEntiteta, int entitetId,
            TStatus? stariStatus, TStatus noviStatus,
            DateTime sada, int? korisnikId, string? napomena = null)
            where TStatus : struct, Enum
        {
            var ocekivaniTip = tipEntiteta switch
            {
                TipEntitetaIstorije.Narudzbina => typeof(StatusNarudzbine),
                TipEntitetaIstorije.Faktura => typeof(StatusFakture),
                TipEntitetaIstorije.Paket => typeof(StatusPaketa),
                TipEntitetaIstorije.Tura => typeof(StatusTure),
                TipEntitetaIstorije.Isporuka => typeof(StatusIsporuke),
                _ => null
            };
            if (ocekivaniTip is null || typeof(TStatus) != ocekivaniTip)
                throw new DomainException($"Status tipa '{typeof(TStatus).Name}' ne odgovara entitetu '{tipEntiteta}'.");
            if (entitetId <= 0)
                throw new DomainException("Istorija se beleži tek za sačuvan entitet (Id > 0).");
            if (stariStatus.HasValue && stariStatus.Value.Equals(noviStatus))
                throw new DomainException("Stari i novi status su isti.");

            return new IstorijaStatusa
            {
                TipEntiteta = tipEntiteta,
                EntitetId = entitetId,
                StariStatus = stariStatus?.ToString(),
                NoviStatus = noviStatus.ToString(),
                Vreme = sada,
                KorisnikId = korisnikId,
                Napomena = napomena?.Trim()
            };
        }
    }
}
