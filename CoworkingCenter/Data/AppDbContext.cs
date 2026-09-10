// AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using CoworkingCenter.Models;

namespace CoworkingCenter.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Workplace> Workplaces { get; set; }
    public DbSet<Resident> Residents { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<MeetingRoom> MeetingRooms { get; set; }
    public DbSet<MeetingRoomBooking> MeetingRoomBookings { get; set; }
    public DbSet<Payment> Payments { get; set; }
}