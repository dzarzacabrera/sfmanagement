using System.Collections.Generic;
using System.Linq;
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
        [FromServices] IGetAllSkillsQueryHandler handler,
        [FromServices] IIsSkillUsedQueryHandler usedHandler,
        [FromServices] IIdEncryptionService enc)
    {
        var skills = await handler.HandleAsync(new GetAllSkillsQuery(IncludeInactive: true));
        ViewBag.PageTitle = "Skills Catalogue";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Skills", "") };
        ViewBag.UsedPositions = await usedHandler.GetUsedPositionsAsync();
        return View(skills.Select(s => s with { IdEncrypted = enc.Encrypt(s.Id) }).ToList());
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
        [FromForm] string? description,
        [FromServices] ICommandHandler<CreateSkillCommand> handler)
    {
        await handler.HandleAsync(new CreateSkillCommand(name, description ?? ""));
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        [FromQuery] string skillId,
        [FromServices] IGetAllSkillsQueryHandler handler,
        [FromServices] IIsSkillUsedQueryHandler usedHandler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(skillId, out var sid)) return NotFound();
        var skills = await handler.HandleAsync(new GetAllSkillsQuery(IncludeInactive: true));
        var skill = skills.FirstOrDefault(s => s.Id == sid);
        if (skill is null) return NotFound();
        ViewBag.PageTitle = "Edit Skill";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Skills", "/Skill/Index"), new("Edit", "") };
        ViewBag.IsSkillUsed = await usedHandler.HandleAsync(new IsSkillUsedQuery(skill.VectorPosition));
        return View(skill with { IdEncrypted = enc.Encrypt(sid) });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(
        [FromForm] string skillIdEncrypted,
        [FromForm] string name,
        [FromForm] string? description,
        [FromServices] ICommandHandler<UpdateSkillCatalogueCommand> handler,
        [FromServices] IGetAllSkillsQueryHandler skillHandler,
        [FromServices] IIsSkillUsedQueryHandler usedHandler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(skillIdEncrypted, out var sid)) return NotFound();
        var skills = await skillHandler.HandleAsync(new GetAllSkillsQuery(IncludeInactive: true));
        var skill = skills.FirstOrDefault(s => s.Id == sid);
        if (skill is null) return NotFound();
        if (await usedHandler.HandleAsync(new IsSkillUsedQuery(skill.VectorPosition)))
        {
            TempData["ToastError"] = "This skill has been used in evaluations or assignments and cannot be edited.";
            return RedirectToAction("Edit", new { skillId = skillIdEncrypted });
        }
        await handler.HandleAsync(new UpdateSkillCatalogueCommand(sid, name, description ?? ""));
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Detail(
        [FromQuery] string skillId,
        [FromServices] IGetAllSkillsQueryHandler handler,
        [FromServices] IIsSkillUsedQueryHandler usedHandler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(skillId, out var sid)) return NotFound();
        var skills = await handler.HandleAsync(new GetAllSkillsQuery(IncludeInactive: true));
        var skill = skills.FirstOrDefault(s => s.Id == sid);
        if (skill is null) return NotFound();
        ViewBag.PageTitle = skill.Name;
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Skills", "/Skill/Index"), new("Detail", "") };
        ViewBag.IsSkillUsed = await usedHandler.HandleAsync(new IsSkillUsedQuery(skill.VectorPosition));
        return View(skill with { IdEncrypted = enc.Encrypt(sid) });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(
        [FromForm] string skillIdEncrypted,
        [FromServices] ICommandHandler<ToggleSkillActiveCommand> handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(skillIdEncrypted, out var sid)) return NotFound();
        await handler.HandleAsync(new ToggleSkillActiveCommand(sid));
        return RedirectToAction("Index");
    }
}
