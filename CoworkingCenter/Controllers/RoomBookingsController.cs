using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // ОБЯЗАТЕЛЬНО: добавили для работы .Include()
using CoworkingCenter.Data;
using CoworkingCenter.Models;
using CoworkingCenter.Services;

namespace CoworkingCenter.Controllers;

public class RoomBookingsController : Controller
{
    private readonly AppDbContext _context;
    private readonly CoworkingService _service;

    public RoomBookingsController(AppDbContext context, CoworkingService service)
    {
        _context = context;
        _service = service;
    }

    // Список всех бронирований С ИМЕНАМИ РЕЗИДЕНТОВ
    public IActionResult Index(int page = 1)
    {
        if (page < 1) page = 1;
        int pageSize = 15;

        int totalItems = _context.MeetingRoomBookings.Count();

        var bookings = _context.MeetingRoomBookings
            .Include(b => b.Resident)
            .OrderByDescending(b => b.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        return View(bookings);
    }

    // Открытие формы (Передаем список резидентов для удобного выбора по ФИО)
    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Residents = _context.Residents.ToList();
        return View();
    }

    // Сохранение бронирования с проверкой времени и автоматической оплатой
    [HttpPost]
public IActionResult Create(MeetingRoomBooking booking, int residentSelectionType, int? residentId, string? temporaryName, decimal calculatedPrice, string startTimeStr, string endTimeStr)
{
    // Безопасно парсим строки времени из браузера
    if (TimeSpan.TryParse(startTimeStr, out TimeSpan parsedStart)) booking.StartTime = parsedStart;
    if (TimeSpan.TryParse(endTimeStr, out TimeSpan parsedEnd)) booking.EndTime = parsedEnd;

    // 🔥 АБСОЛЮТНАЯ ПРОВЕРКА ОВЕРБУКИНГА: Ищем наложение прямо в таблице бронирований комнат
    bool isRoomBusy = _context.MeetingRoomBookings.Any(b =>
        b.MeetingRoomId == booking.MeetingRoomId && // Та же переговорка (1-10)
        b.Date.Date == booking.Date.Date &&         // Тот же день календаря
        booking.StartTime < b.EndTime &&            // Время накладывается
        booking.EndTime > b.StartTime
    );

    if (isRoomBusy)
    {
        // Если комната занята — немедленный разрыв, выводим красную ошибку
        ModelState.AddModelError("", $"❌ Ошибка бронирования: Переговорная комната №{booking.MeetingRoomId} на указанный интервал времени уже занята!");
        ViewBag.Residents = _context.Residents.ToList();
        return View(booking); //Код останавливается, дубликат в базу НЕ пойдет
    }

    // --- ЕСЛИ КОМНАТА СВОБОДНА, ОФОРМЛЯЕМ ЗАПИСЬ ---

    int finalResidentId = 0;
    string residentNameForReceipt = "";

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
            Name = string.IsNullOrEmpty(temporaryName) ? "Частное лицо (Разовый визит)" : temporaryName,
            Contacts = "Переговорная комната",
            Tariff = "Разовый визит",
            TariffEndDate = booking.Date
        };
        _context.Residents.Add(tempResident);
        _context.SaveChanges();
        finalResidentId = tempResident.Id;
        residentNameForReceipt = tempResident.Name;
    }

    // Сохраняем проверенную бронь переговорной
    booking.ResidentId = finalResidentId;
    _context.MeetingRoomBookings.Add(booking);

    // Считаем деньги и выбиваем чек в оплаты
    double hours = (booking.EndTime - booking.StartTime).TotalHours;
    if (hours <= 0) hours = 1;
    decimal totalAmount = calculatedPrice * (decimal)hours;

    var payment = new Payment
    {
        Date = DateTime.Now,
        Amount = totalAmount,
        Service = "Аренда переговорной комнаты №" + booking.MeetingRoomId + " на " + hours.ToString("F1") + " ч. Клиент: " + residentNameForReceipt,
        ResidentId = finalResidentId
    };
    _context.Payments.Add(payment);
    
    _context.SaveChanges(); // Фиксируем в MS SQL Server

    return RedirectToAction("Index");
}


    public IActionResult Delete(int id)
    {
        var booking = _context.MeetingRoomBookings.Find(id);
        if (booking != null)
        {
            _context.MeetingRoomBookings.Remove(booking);
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }
}
