using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Включаем поддержку только контроллеров и представлений (MVC)
builder.Services.AddControllersWithViews();

// Настройка базы данных MS SQL Server
builder.Services.AddDbContext<CoworkingCenter.Data.AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрация нашего сервиса бизнес-логики
builder.Services.AddScoped<CoworkingCenter.Services.CoworkingService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// Настройка главного маршрута — проект ВСЕГДА будет открывать резидентов
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Residents}/{action=Index}/{id?}");

// АВТОМАТИЧЕСКАЯ ИНИЦИАЛИЗАЦИЯ И ЗАПОЛНЕНИЕ 10 000+ ЗАПИСЕЙ:
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CoworkingCenter.Data.AppDbContext>();
        // Вызываем наш метод генерации
        CoworkingCenter.Data.DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        // Если что-то пойдет не так, программа запишет ошибку в лог, но не упадет
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка автозаполнения базы данных демо-данными.");
    }
}


app.Run();