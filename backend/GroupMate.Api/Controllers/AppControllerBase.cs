using GroupMate.Api.Data;
using GroupMate.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroupMate.Api.Controllers;

/// <summary>
/// Kopīgā bāzes klase kontrolieriem.
///
/// PROTOTIPA VIENKĀRŠOJUMS: īstas autentifikācijas (e-pasts/parole, SSO caur Keycloak) vietā
/// priekšpuse sūta izvēlētā demonstrācijas lietotāja ID galvenē "X-User-Id".
/// Lomu pārbaude (piekļuves kontrole pēc lomas) gan tiek veikta serverī tāpat kā pilnajā sistēmā.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class AppControllerBase : ControllerBase
{
    protected readonly AppDbContext Db;

    protected AppControllerBase(AppDbContext db)
    {
        Db = db;
    }

    protected static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    protected async Task<User?> CurrentUserAsync()
    {
        if (!Request.Headers.TryGetValue("X-User-Id", out var value) || !int.TryParse(value.ToString(), out var id))
            return null;

        return await Db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsBlocked);
    }

    protected ObjectResult Error(int statusCode, string message) =>
        StatusCode(statusCode, new { message });

    protected ObjectResult NotLoggedIn() => Error(401, "Lietotājs nav autorizējies.");

    protected ObjectResult NoAccess(string message = "Nav piekļuves tiesību šai darbībai.") => Error(403, message);

    protected ObjectResult Validation(string message) => Error(400, message);

    protected ObjectResult NotFoundMessage(string message) => Error(404, message);
}
