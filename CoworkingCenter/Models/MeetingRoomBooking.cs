// MeetingRoomBooking.cs - Бронирование переговорной (нужно для доп. функции)
namespace CoworkingCenter.Models;

public class MeetingRoomBooking
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    
    public int MeetingRoomId { get; set; }
    public int ResidentId { get; set; }
    
    public Resident? Resident { get; set; } 
}