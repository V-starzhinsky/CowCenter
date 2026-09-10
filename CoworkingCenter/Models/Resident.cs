// Resident.cs - Резидент
namespace CoworkingCenter.Models;

public class Resident
{
    public int Id { get; set; }
    public string Name { get; set; } = "";         // ФИО или Компания
    public string Contacts { get; set; } = "";     // Контакты
    public string Tariff { get; set; } = "";       // Тариф (абонемент)
    public DateTime TariffEndDate { get; set; }    // Дата окончания тарифа
    
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();  //Отношение 1 ко многим: у 1 резидента много броней
}