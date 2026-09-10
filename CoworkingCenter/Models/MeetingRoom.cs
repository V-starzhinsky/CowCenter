// MeetingRoom.cs - Переговорная комната
namespace CoworkingCenter.Models;

public class MeetingRoom
{
    public int Id { get; set; }
    public int Capacity { get; set; }             // Вместимость
    public string Equipment { get; set; } = "";    // Оборудование
    public decimal Price { get; set; }             // Цена в час
}