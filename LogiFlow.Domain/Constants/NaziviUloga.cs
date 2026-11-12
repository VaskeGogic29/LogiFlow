namespace LogiFlow.Domain.Constants
{
    //KLASA SLUZI DA CUVA STRINGOVE DA SE NAZIVI ULOGA NE BI PISALI RUCNO
    public static class NaziviUloga
    {
        public const string Klijent = "Klijent";
        public const string Operativac = "Operativac";
        public const string Vozac = "Vozac";
        public const string Sef = "Sef";
        public const string Administrator = "Administrator";

        public static readonly IReadOnlyList<string> Sve =
            [Klijent, Operativac, Vozac, Sef, Administrator];
    }
}
