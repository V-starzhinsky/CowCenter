namespace CoworkingCenter.Models;

public class Workplace
{
    public int Id { get; set; }
    public string Type { get; set; } = "";         // фикс / флекс / офис
    public string TableNumber { get; set; } = "";  // Номер стола
    public string Equipment { get; set; } = "";    // Оснащение
    public decimal Price { get; set; }             // Цена в час/день
    
}