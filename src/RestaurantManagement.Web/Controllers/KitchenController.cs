using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Web.Controllers;

public class KitchenController : Controller
{
    private readonly IKOTService _kotService;

    public KitchenController(IKOTService kotService)
    {
        _kotService = kotService;
    }

    public async Task<IActionResult> Index()
    {
        var kots = await _kotService.GetActiveKOTsAsync();
        return View(kots);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateKOTStatus(int kotId, KOTStatus status)
    {
        await _kotService.UpdateKOTStatusAsync(kotId, status);
        return RedirectToAction(nameof(Index));
    }
}
