namespace LogiFlow.Domain.Exceptions
{
    public class InvalidStatusTransitionException : DomainException
    {
        public string Entitet { get; }
        public string TrenutniStatus { get; }
        public string ZeljeniStatus { get; }

        public InvalidStatusTransitionException(string entitet, Enum trenutni, Enum zeljeni)
            : base($"{entitet}: prelaz iz statusa '{trenutni}' u '{zeljeni}' nije dozvoljen.")
        {
            Entitet = entitet;
            TrenutniStatus = trenutni.ToString();
            ZeljeniStatus = zeljeni.ToString();
        }
    }
}
