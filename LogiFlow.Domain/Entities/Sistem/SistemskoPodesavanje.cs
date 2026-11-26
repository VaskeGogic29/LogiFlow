using LogiFlow.Domain.Entities.Common;
using LogiFlow.Domain.Enums;
using LogiFlow.Domain.Exceptions;
using System.Globalization;

namespace LogiFlow.Domain.Entities.Sistem
{
    public class SistemskoPodesavanje : BaseEntity
    {
        public string Kljuc { get; private set; } = null!;      // npr. "Faktura.RokPlacanjaDana", unique
        public string Vrednost { get; private set; } = null!;   // uvek tekst, tumaci se po Tip
        public TipPodesavanja Tip { get; private set; }
        public string? Opis { get; private set; }

        private SistemskoPodesavanje() { } // za EF Core

        public SistemskoPodesavanje(string kljuc, string vrednost, TipPodesavanja tip, string? opis = null)
        {
            if (string.IsNullOrWhiteSpace(kljuc))
                throw new DomainException("Ključ podešavanja je obavezan.");
            if (!Enum.IsDefined(tip))
                throw new DomainException("Nepoznat tip podešavanja.");

            Kljuc = kljuc.Trim();
            Tip = tip;
            Opis = opis?.Trim();
            PromeniVrednost(vrednost);
        }

        public void PromeniVrednost(string novaVrednost)
        {
            if (string.IsNullOrWhiteSpace(novaVrednost))
                throw new DomainException("Vrednost podešavanja je obavezna.");

            var vrednost = novaVrednost.Trim();
            if (!JeIspravna(vrednost, Tip))
                throw new DomainException($"Vrednost '{vrednost}' nije ispravna za tip '{Tip}'.");
            Vrednost = vrednost;
        }

        public void PromeniOpis(string? opis) => Opis = opis?.Trim();

        // ---------- Citanje u pravom tipu ----------

        public int KaoCeoBroj()
        {
            ProveriTip(TipPodesavanja.CeoBroj);
            return int.Parse(Vrednost, CultureInfo.InvariantCulture);
        }

        public decimal KaoDecimalni()
        {
            ProveriTip(TipPodesavanja.Decimalni);
            return decimal.Parse(Vrednost, DecimalniStil, CultureInfo.InvariantCulture);
        }

        public bool KaoLogicki()
        {
            ProveriTip(TipPodesavanja.Logicki);
            return bool.Parse(Vrednost);
        }

        public TimeOnly KaoVreme()
        {
            ProveriTip(TipPodesavanja.Vreme);
            return TimeOnly.ParseExact(Vrednost, FormatVremena, CultureInfo.InvariantCulture);
        }

        // ---------- Pomocne ----------

        private const string FormatVremena = "HH:mm";
        private const NumberStyles DecimalniStil = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

        private void ProveriTip(TipPodesavanja ocekivani)
        {
            if (Tip != ocekivani)
                throw new DomainException($"Podešavanje '{Kljuc}' je tipa '{Tip}', a ne '{ocekivani}'.");
        }

        private static bool JeIspravna(string vrednost, TipPodesavanja tip) => tip switch
        {
            TipPodesavanja.Tekst => true,
            TipPodesavanja.CeoBroj => int.TryParse(vrednost, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            TipPodesavanja.Decimalni => decimal.TryParse(vrednost, DecimalniStil, CultureInfo.InvariantCulture, out _),
            TipPodesavanja.Logicki => bool.TryParse(vrednost, out _),
            TipPodesavanja.Vreme => TimeOnly.TryParseExact(vrednost, FormatVremena, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            _ => false
        };
    }
}
