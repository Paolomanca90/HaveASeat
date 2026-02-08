using Google.Apis.Auth.AspNetCore3;
using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.Services;
using HaveASeat.Utilities.Roles;
using HaveASeat.Utilities.Subscriptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISlotService, SlotService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IGiftCardPdfService, GiftCardPdfService>();
builder.Services.AddScoped<ISlotReservationService, SlotReservationService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddHostedService<SlotReservationCleanupService>();
builder.Services.AddHostedService<BookingReminderService>();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
	options.SignIn.RequireConfirmedAccount = false;
	options.Password.RequireDigit = true;
	options.Password.RequiredLength = 8;
})
	.AddRoles<IdentityRole>()
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddDefaultTokenProviders();

builder.Services.AddAuthentication()
	.AddCookie()
	.AddGoogleOpenIdConnect(options =>
	{
		IConfigurationSection googleAuthNSection = builder.Configuration.GetSection("Google+");
		options.ClientId = googleAuthNSection["ClientId"];
		options.ClientSecret = googleAuthNSection["SecretId"];
	});

builder.Services.PostConfigure<AuthenticationOptions>(options =>
{
	foreach (var scheme in options.Schemes)
	{
		if (scheme.Name == GoogleOpenIdConnectDefaults.AuthenticationScheme)
		{
			scheme.DisplayName = "Accedi con Google";
		}
	}
});

builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.ConfigureApplicationCookie(options =>
{
	options.ExpireTimeSpan = TimeSpan.FromMinutes(180);
	options.SlidingExpiration = true;
	options.Events.OnValidatePrincipal = async context =>
	{
		var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
		var user = await userManager.GetUserAsync(context.Principal);

		if (user != null)
		{
			var roles = await userManager.GetRolesAsync(user);
			var identity = new ClaimsIdentity();
			foreach (var role in roles)
			{
				identity.AddClaim(new Claim(ClaimTypes.Role, role));
			}

			context.Principal.AddIdentity(identity);
		}
		else
		{
			context.RejectPrincipal();
			return;
		}
	};
});
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddHttpClient();
StripeConfiguration.ApiKey = builder.Configuration.GetValue<string>("Stripe:SecretKey");

builder.Services.Configure<IISServerOptions>(options =>
{
	options.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

builder.Services.Configure<FormOptions>(options =>
{
	options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	try
	{
		var context = services.GetRequiredService<ApplicationDbContext>();
		await context.Database.MigrateAsync();

		await SubscriptionsInitializer.InitializeSubscriptions(context);
		await RolesInitializer.InitializeRoles(services);
	}
	catch (Exception ex)
	{
		var logger = services.GetRequiredService<ILogger<Program>>();
		logger.LogError(ex, "Errore durante l'inizializzazione");
	}
}

CreateUploadDirectories(app.Environment);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static void CreateUploadDirectories(IWebHostEnvironment environment)
{
	var uploadsPath = Path.Combine(environment.WebRootPath, "uploads");
	var galleryPath = Path.Combine(uploadsPath, "gallery");
	var logosPath = Path.Combine(uploadsPath, "logos");

	Directory.CreateDirectory(galleryPath);
	Directory.CreateDirectory(logosPath);
}
