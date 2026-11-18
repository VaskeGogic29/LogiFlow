namespace LogiFlow.Domain.Enums
{
    public enum StatusPaketa
    {
        UPripremi = 1,           // pakovanje
        SadrzajPotvrdjen = 2,    // kutija zakljucana
        SpremanZaTransport = 3,
        DodeljenTuri = 4,
        UVozilu = 5,
        Isporucen = 6,
        NijeIsporucen = 7,
        VracenUMagacin = 8,
        Otkazan = 9              // ponisten dok je bio u pripremi
    }
}
