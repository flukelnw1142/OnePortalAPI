using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;
using OnePortal_Api.Services;
using Microsoft.AspNetCore.Http.Features;
using OnePortal_Api.Helpers;
using OnePortal_Api.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<OracleDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection")));
builder.Services.AddSingleton<OracleDirectService>();
builder.Services.AddSingleton<DatabaseHelper>();

builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IRoleService, RoleService>();
builder.Services.AddTransient<ICustomerService, CustomerService>();
builder.Services.AddTransient<ISupplierService, SupplierService>();
builder.Services.AddTransient<IPostCodeServie, PostCodeService>();
builder.Services.AddTransient<ISupplierTypeMasterDataService, SupplierTypeMasterDataService>();
builder.Services.AddTransient<ICustomerTypeMasterDataService, CustomerTypeMasterDataService>();
builder.Services.AddTransient<ISupplierBankService, SupplierBankService>();
builder.Services.AddTransient<IBankMasterDataService, BankMasterDataService>();
builder.Services.AddTransient<IEventLogService, EventLogService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IGroupService, GroupService>();
builder.Services.AddTransient<ISupplierFileService, SupplierFileService>();
builder.Services.AddTransient<ITempNumKeyService, TempNumKeyService>();
builder.Services.AddScoped<IWatermarkService, WatermarkServiceAspose>();

builder.Services.AddScoped<CustomAuthorizationFilter>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100_000_000;
    options.ValueCountLimit = int.MaxValue;
    options.ValueLengthLimit = int.MaxValue;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policyBuilder =>
    {
        policyBuilder.WithOrigins(
            "http://10.10.0.28:8085",
            "http://localhost:2222",
            "http://localhost:8085",
            "http://localhost:5277",
            "https://localhost:7126"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\n\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR...\""
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseCors("AllowSpecificOrigin");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.DefaultModelsExpandDepth(-1);
    });
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();