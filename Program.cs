using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelAgenda.Data;
using TravelAgenda.Models;
using TravelAgenda.Repositories;
using TravelAgenda.Repositories.Interfaces;
using TravelAgenda.Services;
using TravelAgenda.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("TravelAgenda")));


builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>(); builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("TravelAgenda")));
builder.Services.AddAuthentication()
   .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
   {
	   options.ClientId = builder.Configuration.GetSection("GoogleKeys:ClientId").Value;
	   options.ClientSecret = builder.Configuration.GetSection("GoogleKeys:ClientSecret").Value;

	   options.Scope.Add("https://www.googleapis.com/auth/calendar");
	   options.Scope.Add("https://www.googleapis.com/auth/calendar.events");
	   options.SaveTokens = true;
	   options.AccessType = "offline";

	   options.Events.OnRedirectToAuthorizationEndpoint = context =>
	   {
		   context.HttpContext.Response.Redirect(context.RedirectUri + "&prompt=consent");
		   return Task.CompletedTask;
	   };

	   // Store tokens in database
	   options.Events.OnTicketReceived = async context =>
	   {
		   var dbContext = context.HttpContext.RequestServices.GetService<ApplicationDbContext>();
		   var userManager = context.HttpContext.RequestServices.GetService<UserManager<IdentityUser>>();

		   // Get user by external login info
		   var loginProvider = context.Scheme.Name;
		   var providerKey = context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

		   if (!string.IsNullOrEmpty(providerKey))
		   {
			   var user = await userManager.FindByLoginAsync(loginProvider, providerKey);
			   if (user != null)
			   {
				   var tokens = context.Properties.GetTokens();
				   var accessToken = tokens.FirstOrDefault(t => t.Name == "access_token")?.Value;
				   var refreshToken = tokens.FirstOrDefault(t => t.Name == "refresh_token")?.Value;
				   var expiresAt = tokens.FirstOrDefault(t => t.Name == "expires_at")?.Value;

				   if (!string.IsNullOrEmpty(accessToken))
				   {
					   var existingToken = await dbContext.UserGoogleTokens
						   .FirstOrDefaultAsync(t => t.UserId == user.Id);

					   if (existingToken != null)
					   {
						   existingToken.AccessToken = accessToken;
						   existingToken.RefreshToken = refreshToken ?? existingToken.RefreshToken;
						   existingToken.ExpiresAt = DateTime.TryParse(expiresAt, out var exp) ? exp : DateTime.UtcNow.AddHours(1);
						   existingToken.UpdatedAt = DateTime.UtcNow;
					   }
					   else
					   {
						   dbContext.UserGoogleTokens.Add(new UserGoogleToken
						   {
							   UserId = user.Id,
							   AccessToken = accessToken,
							   RefreshToken = refreshToken,
							   ExpiresAt = DateTime.TryParse(expiresAt, out var exp) ? exp : DateTime.UtcNow.AddHours(1),
							   CreatedAt = DateTime.UtcNow,
							   UpdatedAt = DateTime.UtcNow
						   });
					   }

					   await dbContext.SaveChangesAsync();
				   }
			   }
		   }
	   };
   });
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();


builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();

builder.Services.AddScoped<IUserInfoService, UserInfoService>();
builder.Services.AddScoped<IUserInfoRepository, UserInfoRepository>();

builder.Services.AddScoped<ISchedule_ActivityService, Schedule_ActivityService>();
builder.Services.AddScoped<ISchedule_ActivityRepository, Schedule_ActivityRepository>();
builder.Services.AddScoped<IGoogleCalendarService, GoogleCalendarService>();



builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IGoogleTokenService, GoogleTokenService>();


builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = false;
    options.Password.RequiredUniqueChars = 6;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
});
builder.Services.Configure<AuthMessageSenderOptions>(options =>
{
	options.SendGridKey = builder.Configuration["SendGridAPI:ApiKey"];
});

builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddHttpContextAccessor();


/*builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccesDenied";
    options.SlidingExpiration = true;
});*/

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Index/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
