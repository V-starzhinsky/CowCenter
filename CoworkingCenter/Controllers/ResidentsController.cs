using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoworkingCenter.Data;
using CoworkingCenter.Models;
using CoworkingCenter.Services;
using System;
using System.Linq;

namespace CoworkingCenter.Controllers;

public class ResidentsController : Controller
{
    private readonly AppDbContext _context;
    private readonly CoworkingService _service; // 🔥 ГЛОБАЛЬНОЕ ПОЛЕ ОБЪЯВЛЕНО ТЕПЕРЬ ТУТ

    // Конструктор: внедряем и базу, и наш сервис бизнес-логики
    public ResidentsController(AppDbContext context, CoworkingService service)
    {
        _context = context;
        _service = service;
    }

    // 1. Отображение списка всех резидентов (по 15 записей на страницу)
    public IActionResult Index(int page = 1)
    {
        if (page < 1) page = 1;
        int pageSize = 15;

        int totalItems = _context.Residents.Count();

        var residents = _context.Residents
            .Include(r => r.Bookings)
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        return View(residents);
    }

    // 2. Безопасная детальная карточка резидента и расчет часов
    public IActionResult Details(int id)
    {
        // 1. Ищем резидента по его ID без тяжелых сквозных связей ThenInclude
        var resident = _context.Residents.Find(id);
        if (resident == null) return NotFound();

        // 2. Отдельно вытаскиваем из базы только бронирования этого резидента
        // Добавляем .Take(15), чтобы страница грузилась мгновенно, даже если у него 500 броней!
        var bookings = _context.Bookings
            .Where(b => b.ResidentId == id)
            .OrderByDescending(b => b.Date)
            .Take(15) 
            .ToList();

        // Вручную подтягиваем информацию о столах, только если они физически существуют
        foreach (var b in bookings)
        {
            b.Workplace = _context.Workplaces.Find(b.WorkplaceId);
        }

        // Кладем список броней в сам объект резидента, чтобы страница его увидела
        resident.Bookings = bookings;

        // 3. Вызываем наш исправленный метод подсчета часов
        double hours = _service.GetTotalHoursByResident(id);
        ViewBag.TotalHours = hours; 

        return View(resident);
    }


    // 3. Добавление (Форма)
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // 3. Добавление (Сохранение в базу)
    [HttpPost]
    public IActionResult Create(Resident resident)
    {
        _context.Residents.Add(resident);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    // 4. Редактирование (Форма)
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var resident = _context.Residents.Find(id);
        if (resident == null) return NotFound();
        return View(resident);
    }

    // 4. Редактирование (Обновление)
    [HttpPost]
    public IActionResult Edit(Resident resident)
    {
        _context.Residents.Update(resident);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    // 5. Удаление резидента
    public IActionResult Delete(int id)
    {
        var resident = _context.Residents.Find(id);
        if (resident != null)
        {
            _context.Residents.Remove(resident);
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }

    // Доп. функция 2: Отображение только активных резидентов
    public IActionResult Active()
    {
        var activeResidents = _service.GetActiveResidents();
        return View("Index", activeResidents);
    }
}
