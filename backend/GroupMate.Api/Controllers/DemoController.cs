using GroupMate.Api.Data;
using GroupMate.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroupMate.Api.Controllers;

/// <summary>Tikai prototipam: demonstrācijas lietotāju saraksts un datubāzes atjaunošana.</summary>
[Route("api/demo")]
public class DemoController : AppControllerBase
{
    public DemoController(AppDbContext db) : base(db) { }

    [HttpGet("users")]
    public async Task<ActionResult<List<DemoUserDto>>> Users()
    {
        var users = await Db.Users
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        return users
            .Select(u => new DemoUserDto(u.Id, u.FullName, u.Email, u.Role.ToString(), u.PreferredRole))
            .ToList();
    }

    /// <summary>Iztīra visas tabulas un ievieto sākotnējos datus (ērtībai, ierakstot video).</summary>
    [HttpPost("reset")]
    public IActionResult Reset()
    {
        Db.Database.ExecuteSqlRaw(
            "TRUNCATE TABLE \"Notifications\", \"Feedback\", \"TeamMembers\", \"Teams\", \"Applications\", " +
            "\"ProjectSkills\", \"Projects\", \"Availability\", \"UserSkills\", \"Skills\", \"Users\" " +
            "RESTART IDENTITY CASCADE;");
        Db.ChangeTracker.Clear();
        SeedData.Seed(Db);
        return Ok(new { message = "Datubāze atjaunota sākotnējā stāvoklī." });
    }
}
