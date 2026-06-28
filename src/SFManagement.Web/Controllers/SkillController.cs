using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;

namespace SFManagement.Web.Controllers;

public class SkillController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromServices] IGetAllSkillsQueryHandler handler)
    {
        var skills = await handler.HandleAsync(new GetAllSkillsQuery(IncludeInactive: true));
        ViewBag.PageTitle = "Skills Catalogue";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Skills", "") };
        return View(skills);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.PageTitle = "Add Skill";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Skills", "/Skill/Index"), new("Add Skill", "") };
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromForm] string name,
        [FromServices] ICommandHandler<CreateSkillCommand> handler)
    {
        await handler.HandleAsync(new CreateSkillCommand(name));
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        [FromQuery] int skillId,
        [FromServices] IGetAllSkillsQueryHandler handler)
    {
        var skills = await handler.HandleAsync(new GetAllSkillsQuery(IncludeInactive: true));
        var skill = skills.FirstOrDefault(s => s.Id == skillId);
        if (skill is null) return NotFound();
        ViewBag.PageTitle = "Edit Skill";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Skills", "/Skill/Index"), new("Edit", "") };
        return View(skill);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(
        [FromForm] int skillId,
        [FromForm] string name,
        [FromServices] ICommandHandler<UpdateSkillCatalogueCommand> handler)
    {
        await handler.HandleAsync(new UpdateSkillCatalogueCommand(skillId, name));
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(
        [FromForm] int skillId,
        [FromServices] ICommandHandler<ToggleSkillActiveCommand> handler)
    {
        await handler.HandleAsync(new ToggleSkillActiveCommand(skillId));
        return RedirectToAction("Index");
    }
}
