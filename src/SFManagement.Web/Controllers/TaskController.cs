using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Domain.Enums;
using SFManagement.Web.ViewModels;

namespace SFManagement.Web.Controllers;

public class TaskController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromServices] IGetAllTasksQueryHandler handler)
    {
        var tasks = await handler.HandleAsync(new GetAllTasksQuery());
        ViewBag.PageTitle = "Tasks";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Tasks", "") };
        return View(tasks);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        [FromQuery] int? projectId,
        [FromServices] IGetAllProjectsQueryHandler projectsHandler,
        [FromServices] IGetAllSkillsQueryHandler skillsHandler)
    {
        var projects = await projectsHandler.HandleAsync(new GetAllProjectsQuery());
        var skills = await skillsHandler.HandleAsync(new GetAllSkillsQuery());
        var criticalities = new List<CriticalityOption>
        {
            new(Criticality.Low, "Low"),
            new(Criticality.Medium, "Medium"),
            new(Criticality.High, "High"),
            new(Criticality.Critical, "Critical"),
        };

        ViewBag.AllSkills = skills;
        ViewBag.PageTitle = "Create Task";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Tasks", "/Task/Index"), new("Create Task", "") };

        var defaultProjectId = projectId ?? (projects.Count > 0 ? projects[0].Id : 1);
        return View(new CreateTaskViewModel(defaultProjectId, projects, skills.Select(s => new SkillCatalogueItem(s.Id, s.Name, s.VectorPosition)).ToList(), criticalities));
    }

    [HttpGet]
    public async Task<IActionResult> Detail(
        [FromQuery] int taskId,
        [FromServices] IGetTaskByIdQueryHandler handler)
    {
        var task = await handler.HandleAsync(new GetTaskByIdQuery(taskId));
        if (task is null) return NotFound();

        ViewBag.PageTitle = task.Title;
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Tasks", "/Task/Index"), new(task.Title, "") };
        return View(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromForm] int projectId,
        [FromForm] string title,
        [FromForm] string? description,
        [FromForm] string criticality,
        [FromForm] int[] skillPositions,
        [FromForm] float[] skillLevels,
        [FromServices] ICommandHandler<CreateTaskCommand> handler)
    {
        var vector = new float[1024];
        for (int i = 0; i < skillPositions.Length && i < skillLevels.Length; i++)
        {
            var pos = skillPositions[i];
            if (pos >= 0 && pos < 1024)
                vector[pos] = Math.Clamp(skillLevels[i], 0.0f, 10.0f);
        }

        var command = new CreateTaskCommand(
            projectId, title, description,
            Enum.Parse<Criticality>(criticality, ignoreCase: true),
            vector);
        await handler.HandleAsync(command);
        return RedirectToAction("Index", "Dashboard", new { projectId });
    }
}
