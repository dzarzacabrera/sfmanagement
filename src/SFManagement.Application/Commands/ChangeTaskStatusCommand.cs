using SFManagement.Domain.Enums;

namespace SFManagement.Application.Commands;

public record ChangeTaskStatusCommand(long TaskId, ProjectTaskStatus NewStatus);
