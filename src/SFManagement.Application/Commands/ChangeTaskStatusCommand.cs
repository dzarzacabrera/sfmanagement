using SFManagement.Domain.Enums;

namespace SFManagement.Application.Commands;

public record ChangeTaskStatusCommand(int TaskId, ProjectTaskStatus NewStatus);
