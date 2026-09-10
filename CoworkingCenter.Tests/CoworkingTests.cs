using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using CoworkingCenter.Data;
using CoworkingCenter.Models;
using CoworkingCenter.Controllers;
using CoworkingCenter.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoworkingCenter.Tests;

public class CoworkingTests
{
    // Вспомогательный метод для создания чистой базы данных в оперативной памяти (In-Memory)
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // каждый тест получает уникальную чистую БД
            .Options;

        return new AppDbContext(options);
    }

    // =========================================================================
    // 🧪 ЧАСТЬ 1: МОДУЛЬНЫЕ ТЕСТЫ (UNIT TESTS) НА СЕРВИС БИЗНЕС-ЛОГИКИ
    // =========================================================================

    [Fact]
    public void GetTotalHoursByResident_ShouldSumHoursCorrectly()
    {
        // Arrange (Подготовка данных)
        using var context = GetInMemoryDbContext();
        var service = new CoworkingService(context);

        var resident = new Resident { Id = 1, Name = "Владислав Старжинский" };
        context.Residents.Add(resident);

        // Добавляем 2 бронирования стола: одно на 3 часа (9:00-12:00), второе на 2 часа (14:00-16:00)
        context.Bookings.Add(new Booking { ResidentId = 1, Date = DateTime.Today, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(12, 0, 0) });
        context.Bookings.Add(new Booking { ResidentId = 1, Date = DateTime.Today, StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(16, 0, 0) });
        
        // Добавляем 1 бронь переговорки на 4 часа (10:00-14:00)
        context.MeetingRoomBookings.Add(new MeetingRoomBooking { ResidentId = 1, Date = DateTime.Today, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(14, 0, 0) });
        context.SaveChanges();

        // Act (Выполнение действия)
        double totalHours = service.GetTotalHoursByResident(1);

        // Assert (Проверка результата: 3 + 2 + 4 = 9 часов)
        Assert.Equal(9.0, totalHours);
    }

    [Fact]
    public void GetActiveResidents_ShouldReturnOnlyActiveTariffs()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new CoworkingService(context);

        context.Residents.Add(new Resident { Id = 1, Name = "Активный клиент", TariffEndDate = DateTime.Today.AddDays(5) });
        context.Residents.Add(new Resident { Id = 2, Name = "Просроченный клиент", TariffEndDate = DateTime.Today.AddDays(-2) });
        context.SaveChanges();

        // Act
        var activeResidents = service.GetActiveResidents();

        // Assert (В списке должен остаться только 1 активный резидент)
        Assert.Single(activeResidents);
        Assert.Equal("Активный клиент", activeResidents[0].Name);
    }

    // =========================================================================
    // 🧪 ЧАСТЬ 2: ИНТЕГРАЦИОННЫЕ ТЕСТЫ НА КОНТРОЛЛЕРЫ (ЗАЩИТА ОТ ОВЕРБУКИНГА)
    // =========================================================================

    [Fact]
    public void CreateRoomBooking_WithTimeConflict_ShouldBlockSaveAndReturnError()
    {
        // Arrange (Создаем ситуацию, когда переговорка №1 уже занята с 12:00 до 15:00)
        using var context = GetInMemoryDbContext();
        var service = new CoworkingService(context);
        var controller = new RoomBookingsController(context, service);

        context.MeetingRoomBookings.Add(new MeetingRoomBooking
        {
            MeetingRoomId = 1,
            Date = DateTime.Today,
            StartTime = new TimeSpan(12, 0, 0),
            EndTime = new TimeSpan(15, 0, 0),
            ResidentId = 99
        });
        context.SaveChanges();

        // Попытка забронировать ту же комнату №1 на пересекающееся время (13:00 - 14:00)
        var newConflictBooking = new MeetingRoomBooking
        {
            MeetingRoomId = 1,
            Date = DateTime.Today
        };

        // Act (Вызываем метод сохранения контроллера, передавая пересекающиеся часы строками)
        var result = controller.Create(newConflictBooking, residentSelectionType: 2, residentId: null, temporaryName: "Разовый тест", calculatedPrice: 15.00m, startTimeStr: "13:00", endTimeStr: "14:00") as ViewResult;

        // Assert
        // 1. Контроллер должен вернуть View (а не редирект), сигнализируя об ошибке на форме
        Assert.NotNull(result);
        
        // 2. В ModelState должна появиться запись об ошибке овербукинга
        Assert.False(controller.ModelState.IsValid);
        
        // 3. В базе данных количество записей должно остаться равным 1 (дубликат заблокирован!)
        Assert.Equal(1, context.MeetingRoomBookings.Count());
    }

    [Fact]
    public void CreateWorkplace_WhenFree_ShouldSaveSuccessfullyAndGeneratePayment()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new CoworkingService(context);
        var controller = new WorkplacesController(context, service);

        var workplace = new Workplace { Type = "Фикс", TableNumber = "Стол №1", Price = 10.00m };

        // Act (Оформляем Стол №1 с 10:00 до 14:00 — на 4 часа)
        var result = controller.Create(workplace, residentSelectionType: 2, residentId: null, temporaryName: "Иван Петров", date: DateTime.Today, startTime: new TimeSpan(10, 0, 0), endTime: new TimeSpan(14, 0, 0)) as RedirectToActionResult;

        // Assert
        // 1. Программа должна успешно сохранить данные и перенаправить пользователя на список (Index)
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);

        // 2. В базе данных должна автоматически создаться финансовая оплата за 4 часа (10 BYN * 4 = 40 BYN)
        var payment = context.Payments.FirstOrDefault();
        Assert.NotNull(payment);
        Assert.Equal(40.00m, payment.Amount);
    }
}
