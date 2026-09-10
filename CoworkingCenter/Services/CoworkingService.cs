using Microsoft.EntityFrameworkCore;
using CoworkingCenter.Data;
using CoworkingCenter.Models;

namespace CoworkingCenter.Services;

public class CoworkingService
{
    private readonly AppDbContext _context;

    // Конструктор: получаем доступ к нашей базе данных
    public CoworkingService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Функция 1: Проверка доступности переговорной комнаты на заданное время.
    /// Возвращает true, если комната СВОБОДНА, и false, если уже ЗАНЯТА.
    /// </summary>
    public bool IsRoomAvailable(int roomId, DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        // Ищем в базе бронирования этой комнаты на эту же дату
        var bookings = _context.MeetingRoomBookings
            .Where(b => b.MeetingRoomId == roomId && b.Date.Date == date.Date)
            .ToList();

        // Проверяем каждое бронирование на пересечение по времени
        foreach (var booking in bookings)
        {
            // Пересечение происходит, если новое начало раньше существующего конца
            // И одновременно новый конец позже существующего начала
            if (startTime < booking.EndTime && endTime > booking.StartTime)
            {
                return false; // Найдено пересечение, комната занята
            }
        }

        return true; // Пересечений нет, комната свободна
    }

    /// <summary>
    /// Функция 2: Список резидентов с активными абонементами.
    /// Выбирает тех, у кого дата окончания тарифа больше или равна сегодняшней.
    /// </summary>
    public List<Resident> GetActiveResidents()
    {
        DateTime today = DateTime.Today;

        // Фильтруем резидентов по дате окончания абонемента
        var activeResidents = _context.Residents
            .Where(r => r.TariffEndDate >= today)
            .ToList();

        return activeResidents;
    }

    /// <summary>
    /// Функция 3: Учет времени аренды по каждому резиденту.
    /// Считает суммарное количество часов (места + переговорки) для конкретного резидента.
    /// </summary>
    public double GetTotalHoursByResident(int residentId)
    {
        // 1. Извлекаем из базы только бронирования рабочих мест этого конкретного резидента
        var workplaceBookings = _context.Bookings
            .Where(b => b.ResidentId == residentId)
            .ToList();

        // Считаем сумму часов по рабочим местам
        double workplaceHours = workplaceBookings
            .Select(b => (b.EndTime - b.StartTime).TotalHours)
            .Sum();

        // 2. Извлекаем из базы только бронирования переговорных комнат этого конкретного резидента
        var roomBookings = _context.MeetingRoomBookings
            .Where(b => b.ResidentId == residentId)
            .ToList();

        // Считаем сумму часов по переговорным комнатам
        double roomHours = roomBookings
            .Select(b => (b.EndTime - b.StartTime).TotalHours)
            .Sum();

        // Возвращаем итоговую сумму наработанного времени
        return workplaceHours + roomHours;
    }
}
