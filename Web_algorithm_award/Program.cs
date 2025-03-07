using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Web_algorithm_award_DataAccess.Data;
using Web_algorithm_award_Model;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình ngôn ngữ mặc định là Tiếng Việt
var cultureInfo = new CultureInfo("vi-VN");
cultureInfo.NumberFormat.CurrencySymbol = "₫";
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Cấu hình MVC
builder.Services.AddControllersWithViews();

// Cấu hình Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Không yêu cầu xác nhận email khi đăng ký
})
    .AddEntityFrameworkStores<ApplicationDbContext>() // Đăng ký UserStore
    .AddDefaultTokenProviders();

// Cấu hình đường dẫn đăng nhập & bị từ chối quyền
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Cấu hình Session (Lưu trữ trong bộ nhớ)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cấu hình phản hồi lỗi ModelState theo JSON
builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    o.InvalidModelStateResponseFactory = actionContext =>
    {
        List<Error> error = actionContext.ModelState
            .Where(modelError => modelError.Value!.Errors.Count > 0)
            .Select(modelError => new Error
            {
                ErrorField = modelError.Key,
                ErrorDescription = modelError.Value!.Errors.FirstOrDefault()!.ErrorMessage
            }).ToList();

        return new BadRequestObjectResult(error);
    };
});

// Thêm Razor Pages (dùng cho Identity UI)
builder.Services.AddRazorPages();

var app = builder.Build();

// Cấu hình Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Bật HTTP Strict Transport Security
}

app.UseSession(); // Sử dụng Session
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Cấu hình Route
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Account}/{action=Login}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.MapRazorPages(); // Hỗ trợ Razor Pages cho Identity UI

app.Run();
