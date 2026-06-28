namespace SFManagement.Application.Abstractions;

public interface ICommandHandler<TCommand>
{
    Task HandleAsync(TCommand command);
}
