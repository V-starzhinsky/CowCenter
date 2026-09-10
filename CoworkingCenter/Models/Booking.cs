// Booking.cs - Бронирование
namespace CoworkingCenter.Models;

public class Booking
{
    public int Id { get; set; }
    public DateTime Date { get; set; }             // Дата
    public TimeSpan StartTime { get; set; }        // Время начала
    public TimeSpan EndTime { get; set; }          // Время окончания
    
    public int ResidentId { get; set; }            // Кто забронировал
    
    public Resident? Resident { get; set; } 
    public int WorkplaceId { get; set; }           // Какое место
    
    public Workplace? Workplace { get; set; }       //Связь со столом/кабинетом
}