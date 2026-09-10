using Microsoft.AspNetCore.Mvc;
using CoworkingCenter.Data;
using System.Linq;

namespace CoworkingCenter.Controllers;

public class PaymentsController : Controller
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
    }

    // Отображение списка всех платежей коворкинга
    public IActionResult Index()
    {
        // Подтягиваем из базы список оплат, сортируя их по дате (свежие — сверху)
        var payments = _context.Payments.OrderByDescending(p => p.Date).ToList();
        return View(payments);
    }
}