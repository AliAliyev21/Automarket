namespace AutoMarket.BuildingBlocks.Web.Validation;

// SEC-INP-05: mətn input-ları model binding-dən sonra, validator-dan əvvəl sanitize olunur (ARCHITECTURE §7.2)
public interface ISanitizableRequest<out TRequest>
{
    public TRequest Sanitize();
}
