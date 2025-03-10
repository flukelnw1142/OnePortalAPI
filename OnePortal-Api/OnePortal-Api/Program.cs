using Microsoft.EntityFrameworkCore;
using OnePortal_Api.Data;
using OnePortal_Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins("http://10.10.0.28:8085", "http://localhost:2222", "http://localhost:8085") // ที่อยู่ต้นทางของคุณ
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials();
        });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors("AllowSpecificOrigin");


// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
