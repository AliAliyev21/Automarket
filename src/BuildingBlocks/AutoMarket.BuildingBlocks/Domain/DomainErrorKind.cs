namespace AutoMarket.BuildingBlocks.Domain;

// HTTP-dən asılı deyil; status xəritəsi BuildingBlocks.Web-dədir (CONVENTIONS §4.4)
public enum DomainErrorKind
{
    // Cari vəziyyətlə ziddiyyət (409)
    Conflict,

    // Yanlış dəyər (400)
    Validation,
}
