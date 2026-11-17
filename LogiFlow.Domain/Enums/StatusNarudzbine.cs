namespace LogiFlow.Domain.Enums
{
    public enum StatusNarudzbine
    {
        Kreirana = 1,           // klijent poslao, cek sefa
        Potvrdjena = 2,         // sef potvrdio, faktura izdata
        UPakovanju = 3,         // otvoren prvi paket
        Spakovana = 4,          // pakovanje završeno
        UTransportu = 5,        // tura sa njenom isporukom je krenula
        Isporucena = 6,
        NeuspesnaIsporuka = 7,  // ceka novi pokusaj
        Otkazana = 8
    }
}
