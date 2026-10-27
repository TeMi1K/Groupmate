using GroupMate.Api.Data;
using GroupMate.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroupMate.Api.Controllers;

[Route("api/skills")]
public class SkillsController : AppControllerBase
{
    public SkillsController(AppDbContext db) : base(db) { }

    [HttpGet]
    public async Task<ActionResult<List<SkillDto>>> GetAll()
    {
        return await Db.Skills
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .Select(s => new SkillDto(s.Id, s.Name, s.Category))
            .ToListAsync();
    }

    [HttpGet("roles")]
    public ActionResult<string[]> Roles() => SeedData.AllRoles;
}
