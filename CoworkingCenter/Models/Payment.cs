// Payment.cs - Оплаты
namespace CoworkingCenter.Models;

public class Payment
{
    public int Id { get; set; }
    public DateTime Date { get; set; }             // Дата
    public decimal Amount { get; set; }            // Сумма
    public string Service { get; set; } = "";      // Услуга
    
    public int ResidentId { get; set; }            // Кто оплатил
}