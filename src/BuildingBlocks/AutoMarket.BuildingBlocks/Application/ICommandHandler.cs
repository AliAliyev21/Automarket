namespace AutoMarket.BuildingBlocks.Application;

// MediatR yoxdur: handler DI ilə birbaşa inject olunur (ADR-0010, CONVENTIONS §5.1)
public interface ICommandHandler<in TCommand, TResult>
{
    public Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand>
{
    public Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
