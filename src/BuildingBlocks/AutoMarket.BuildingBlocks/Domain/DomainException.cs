namespace AutoMarket.BuildingBlocks.Domain;

// Domen invariantının pozulması; global exception handler onu ProblemDetails-ə çevirir (ARCHITECTURE §8.1)
public sealed class DomainException : Exception
{
    public DomainException(DomainError error)
        : base(error?.Message)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    public DomainError Error { get; }
}
