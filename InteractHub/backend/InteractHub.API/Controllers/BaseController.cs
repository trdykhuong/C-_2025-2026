using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.API.Controllers;

public class BaseController : ControllerBase
{
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    protected bool   IsAdmin       => User.IsInRole("Admin");
}
