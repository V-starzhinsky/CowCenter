using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoworkingCenter.Data;

namespace CoworkingCenter.Controllers;

public class BookingsController : Controller
{
    private readonly AppDbContext _context;

    public BookingsController(AppDbContext context)
    {
        _context = context;
    }

    // Вывод архива бронирований с пагинацией (по 20 записей на страницу)
    public IActionResult Index(int page = 1)
    {
        if (page < 1) page = 1;
        int pageSize = 20; // сколько строк выводить за раз

        // Считаем общее количество записей в базе (будет 10 100+)
        int totalItems = _context.Bookings.Count();

        // Вытаскиваем только нужную порцию данных с помощью Skip и Take
        var bookings = _context.Bookings
            .Include(b => b.Resident)
            .OrderByDescending(b => b.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Передаем параметры пагинации на страницу через ViewBag
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        return View(bookings);
    }
}