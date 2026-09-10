using Microsoft.AspNetCore.Mvc;
using CoworkingCenter.Data;
using CoworkingCenter.Models;
using CoworkingCenter.Services;
using Microsoft.EntityFrameworkCore;

namespace CoworkingCenter.Controllers;

public class WorkplacesController : Controller
{
    private readonly AppDbContext _context;
    private readonly CoworkingService _service;

    public WorkplacesController(AppDbContext context, CoworkingService service)
    {
        _context = context;
        _service = service;
    }

    // Список всех мест
    public IActionResult Index(int page = 1)
    {
        if (page < 1) page = 1;
        int pageSize = 15;

        int totalItems = _context.Workplaces.Count();

        var workplaces = _context.Workplaces
            .OrderBy(w => w.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        return View(workplaces);
    }

    // Открытие формы создания (Передаем список резидентов для выпадающего списка)
    [HttpGet]
    public IActionResult Create()
    {
        // Берем всех резидентов из базы, чтобы показать в меню выбор
        ViewBag.Residents = _context.Residents.ToList();
        return View();
    }

    // Комплексное сохранение стола с мгновенным бронированием и проверкой
    [HttpPost]
public IActionResult Create(Workplace workplace, int residentSelectionType, int? residentId, string? temporaryName, DateTime date, TimeSpan startTime, TimeSpan endTime)
{
    // 🔥 НОВЫЙ АЛГОРИТМ: Ищем конфликты по тексту "Стол №Х" или "Кабинет №Х" сквозь связь таблиц!
    bool isSeatBusy = _context.Bookings
        .Include(b => b.Workplace) // Подгружаем физические столы из базы
        .Any(b => 
            b.Workplace != null &&
            b.Workplace.TableNumber == workplace.TableNumber && // Проверяем точный номер стола!
            b.Workplace.Type == workplace.Type &&               // Проверяем тип размещения
            b.Date.Date == date.Date &&                          // Проверяем дату
            startTime < b.EndTime &&                             // Наложение: новый старт раньше старого конца
            endTime > b.StartTime                                // Наложение: новый конец позже старого старта
        );

    // Если стол уже занят в эти часы — жестко прерываем код
    if (isSeatBusy)
    {
        ModelState.AddModelError("", $"❌ Ошибка: {workplace.TableNumber} на указанные часы ({startTime:hh\\:mm} - {endTime:hh\\:mm}) уже забронирован другим посетителем!");
        ViewBag.Residents = _context.Residents.ToList();
        return View(workplace); // Возврат на форму, запись в базу НЕ происходит
    }

    // --- ЕСЛИ СВОБОДНО, ЗАПУСКАЕМ СОХРАНЕНИЕ ---
    
    // 1. Сохраняем стол
    _context.Workplaces.Add(workplace);
    _context.SaveChanges(); 

    int finalResidentId = 0;
    string residentNameForReceipt = "";

    // 2. Обрабатываем резидента
    if (residentSelectionType == 1 && residentId.HasValue)
    {
        finalResidentId = residentId.Value;
        var res = _context.Residents.Find(finalResidentId);
        residentNameForReceipt = res != null ? res.Name : "Резидент";
    }
    else
    {
        var tempResident = new Resident
        {
            Name = string.IsNullOrEmpty(temporaryName) ? "Частное лицо (Разовый)" : temporaryName,
            Contacts = "Указаны при визите",
            Tariff = "Разовый визит",
            TariffEndDate = date
        };
        _context.Residents.Add(tempResident);
        _context.SaveChanges(); 
        finalResidentId = tempResident.Id;
        residentNameForReceipt = tempResident.Name;
    }

    // 3. Создаем запись бронирования
    var booking = new Booking
    {
        Date = date,
        StartTime = startTime,
        EndTime = endTime,
        ResidentId = finalResidentId,
        WorkplaceId = workplace.Id
    };
    _context.Bookings.Add(booking);

    // 4. Финансовый чек
    double hours = (endTime - startTime).TotalHours;
    if (hours <= 0) hours = 1;
    decimal totalPaymentAmount = workplace.Price * (decimal)hours;

    var payment = new Payment
    {
        Date = DateTime.Now,
        Amount = totalPaymentAmount,
        Service = "Аренда места (" + workplace.Type + ", " + workplace.TableNumber + ") на " + hours.ToString("F1") + " ч. Клиент: " + residentNameForReceipt,
        ResidentId = finalResidentId
    };
    _context.Payments.Add(payment);
    
    _context.SaveChanges(); // Сохраняем всё транзакцией

    return RedirectToAction("Index");
}




    public IActionResult Delete(int id)
    {
        var workplace = _context.Workplaces.Find(id);
        if (workplace != null)
        {
            _context.Workplaces.Remove(workplace);
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }
}
