using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = "Admin,Manager,KitchenStaff,Cook")]
public class KitchenController : Controller
{
    private readonly IKOTService _kotService;
    private readonly IUnitOfWork _unitOfWork;

    public KitchenController(IKOTService kotService, IUnitOfWork unitOfWork)
    {
        _kotService = kotService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var kots = await _kotService.GetActiveKOTsAsync();
        var settings = await _unitOfWork.RestaurantSettings.GetAllAsync();
        ViewBag.Setting = settings.FirstOrDefault() ?? new RestaurantSetting();
        return View(kots);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateKOTStatus(int kotId, KOTStatus status)
    {
        await _kotService.UpdateKOTStatusAsync(kotId, status);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AcknowledgeKOT(int kotId)
    {
        var kot = await _unitOfWork.KOTs.GetByIdAsync(kotId);
        if (kot != null)
        {
            kot.AcknowledgedByChef = true;
            await _unitOfWork.KOTs.UpdateAsync(kot);
            await _unitOfWork.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
