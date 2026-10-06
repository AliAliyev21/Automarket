namespace AutoMarket.BuildingBlocks.Application;

// Audit üçün sorğu konteksti (SEC-LOG-04). HTTP xaricində (consumer, job) dəyərlər null-dur
public interface IRequestContext
{
    public string? IpAddress { get; }

    public string? UserAgent { get; }
}
