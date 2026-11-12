namespace LogiFlow.Domain.ValueObjects
{
    public sealed record AdresaSnimak(
        string? Naziv,
        string Ulica,
        string Broj,
        string Grad,
        string PostanskiBroj,
        string? Napomena,
        int? ZonaId
     );
}
