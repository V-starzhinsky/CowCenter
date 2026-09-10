using System;
using System.Collections.Generic;
using System.Linq;
using CoworkingCenter.Models;

namespace CoworkingCenter.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // Автоматически создаем базу данных и таблицы, если их нет
        context.Database.EnsureCreated();

        // Проверяем, есть ли уже данные. Если база заполнена — выходим
        if (context.MeetingRooms.Any())
        {
            return;
        }

        // =========================================================================
        // 1. ГЕНЕРАЦИЯ 10 ПЕРЕГОВОРНЫХ КОМНАТ (Используем свойство Price из модели)
        // =========================================================================
        var rooms = new List<MeetingRoom>
        {
            // 4 комнаты по 5 человек (Цена 15 BYN)
            new MeetingRoom { Capacity = 5, Equipment = "Стандарт (Маркерная доска, Спикерфон)", Price = 15.00m },
            new MeetingRoom { Capacity = 5, Equipment = "Стандарт (Маркерная доска, Спикерфон)", Price = 15.00m },
            new MeetingRoom { Capacity = 5, Equipment = "Стандарт (Маркерная доска, Спикерфон)", Price = 15.00m },
            new MeetingRoom { Capacity = 5, Equipment = "Стандарт (Маркерная доска, Спикерфон)", Price = 15.00m },
            
            // 3 комнаты до 8 человек (Цена 25 BYN)
            new MeetingRoom { Capacity = 8, Equipment = "Медиа-комплект (Проектор, Экран, Кликер)", Price = 25.00m },
            new MeetingRoom { Capacity = 8, Equipment = "Медиа-комплект (Проектор, Экран, Кликер)", Price = 25.00m },
            new MeetingRoom { Capacity = 8, Equipment = "Медиа-комплект (Проектор, Экран, Кликер)", Price = 25.00m },
            
            // 3 комнаты по 10 человек (Цена 35 BYN)
            new MeetingRoom { Capacity = 10, Equipment = "Премиум-конференц (ТВ 4К, Система видеосвязи)", Price = 35.00m },
            new MeetingRoom { Capacity = 10, Equipment = "Премиум-конференц (ТВ 4К, Система видеосвязи)", Price = 35.00m },
            new MeetingRoom { Capacity = 10, Equipment = "Премиум-конференц (ТВ 4К, Система видеосвязи)", Price = 35.00m }
        };
        
        context.MeetingRooms.AddRange(rooms);
        context.SaveChanges(); // Сохраняем, чтобы БД сгенерировала ID для комнат

        // =========================================================================
        // 2. РЕЗИДЕНТЫ (105 записей)
        // =========================================================================
        var residents = new List<Resident>();
        for (int i = 1; i <= 105; i++)
        {
            residents.Add(new Resident
            {
                Name = $"Резидент №{i}",
                Contacts = $"+375 (29) 100-22-{i:00}",
                Tariff = i % 3 == 0 ? "Безлимит" : (i % 2 == 0 ? "Стандарт" : "Базовый"),
                TariffEndDate = DateTime.Today.AddMonths(1)
            });
        }
        context.Residents.AddRange(residents);
        context.SaveChanges();

        // =========================================================================
        // 3. РАБОЧИЕ МЕСТА (105 записей)
        // =========================================================================
        var workplaces = new List<Workplace>();
        for (int i = 1; i <= 105; i++)
        {
            workplaces.Add(new Workplace
            {
                Type = i % 3 == 0 ? "Офис" : (i % 2 == 0 ? "Фикс" : "Флекс"),
                TableNumber = i % 3 == 0 ? $"Кабинет №{(i % 8) + 1}" : $"Стол №{(i % 20) + 1}",
                Equipment = i % 3 == 0 ? "Бизнес комплект" : "Базовый комплект",
                Price = i % 3 == 0 ? 25m : 10m
            });
        }
        context.Workplaces.AddRange(workplaces);
        context.SaveChanges();

        // Получаем реальные сгенерированные ID из базы данных
        var residentIds = context.Residents.Select(r => r.Id).ToList();
        var workplaceIds = context.Workplaces.Select(w => w.Id).ToList();

        // =========================================================================
        // 4. БРОНИРОВАНИЯ (10 100 записей) - Оптимизировано через пакетную вставку
        // =========================================================================
        Random rand = new Random();
        var bookings = new List<Booking>();
        
        for (int i = 1; i <= 10100; i++)
        {
            bookings.Add(new Booking
            {
                Date = DateTime.Today.AddDays(-rand.Next(1, 30)),
                StartTime = new TimeSpan(rand.Next(8, 12), 0, 0),
                EndTime = new TimeSpan(rand.Next(14, 19), 0, 0),
                ResidentId = residentIds[rand.Next(0, residentIds.Count)],
                WorkplaceId = workplaceIds[rand.Next(0, workplaceIds.Count)]
            });

            // Сбрасываем пачки в базу каждые 2000 элементов, чтобы не перегружать память
            if (i % 2000 == 0)
            {
                context.Bookings.AddRange(bookings);
                context.SaveChanges();
                bookings.Clear();
            }
        }
        if (bookings.Any())
        {
            context.Bookings.AddRange(bookings);
            context.SaveChanges();
            bookings.Clear();
        }

        // =========================================================================
        // 5. ОПЛАТЫ (500 записей)
        // =========================================================================
        var payments = new List<Payment>();
        for (int i = 1; i <= 500; i++)
        {
            payments.Add(new Payment
            {
                Date = DateTime.Now.AddDays(-i % 30),
                Amount = 15m * rand.Next(2, 6),
                Service = $"Автоматический чек №{i} за услуги коворкинг-центра",
                ResidentId = residentIds[rand.Next(0, residentIds.Count)]
            });
        }
        context.Payments.AddRange(payments);
        context.SaveChanges();
    }
}
