using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyWebApi.Models;
using MyWebApi.Service;
using MyWebApi.Repository;
using MyWebApi.Providers.Interfaces;
using MyWebApi.Providers.OpenAI;
using MyWebApi.Providers.Gemini;

var builder = WebApplication.CreateBuilder(args);

//
// ============================================================
// 1. CONTROLLERS
// ============================================================
//

builder.Services.AddControllers();


//
// ============================================================
// 2. DATABASE - SQL SERVER + EF CORE
// ============================================================
//

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});


//
// ============================================================
// 3. SWAGGER / OPENAPI
// ============================================================
//

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AI Gateway API",
        Version = "v1",
        Description = "API Gateway trung gian giữa ứng dụng và LLM Provider"
    });

    // Swagger hỗ trợ nhập JWT Bearer Token
    options.AddSecurityDefinition("Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Nhập token theo dạng: Bearer {token}"
        });

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference =
                        new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});


//
// ============================================================
// 4. CORS
// ============================================================
//

// Chỉ cần nếu sau này có Frontend chạy trên trình duyệt.
// Có thể thêm các origin khác khi cần.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


//
// ============================================================
// 5. APPLICATION SERVICES
// ============================================================
//

// Authentication / JWT
builder.Services.AddScoped<ITokenService, TokenService>();

// Password hashing
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Sau này thêm các service của AI Gateway tại đây:
//
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IUsageService, UsageService>();
builder.Services.AddMemoryCache();


//
// ============================================================
// 6. REPOSITORIES
// ============================================================
//

// Sau này thêm Repository:
//
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IAiRequestLogRepository, AiRequestLogRepository>();


//
// ============================================================
// 7. LLM PROVIDERS
// ============================================================
//

// Đây là phần đặc trưng của AI Gateway.
//
// builder.Services.AddHttpClient<OpenAIProvider>();
//
// builder.Services.AddScoped<ILLMProvider, OpenAIProvider>();
//

builder.Services.AddHttpClient<GeminiProvider>();
builder.Services.AddScoped<ILLMProvider, GeminiProvider>();
// Không đăng ký khi OpenAIProvider chưa được tạo.


//
// ============================================================
// 8. AUTHENTICATION - JWT
// ============================================================

var secretKey = builder.Configuration["AppSettings:SecretKey"];

if (string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException(
        "AppSettings:SecretKey chưa được cấu hình."
    );
}

var issuer = builder.Configuration["AppSettings:Issuer"];

if (string.IsNullOrWhiteSpace(issuer))
{
    throw new InvalidOperationException(
        "AppSettings:Issuer chưa được cấu hình."
    );
}

var audience = builder.Configuration["AppSettings:Audience"];

if (string.IsNullOrWhiteSpace(audience))
{
    throw new InvalidOperationException(
        "AppSettings:Audience chưa được cấu hình."
    );
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                // Kiểm tra Issuer
                ValidateIssuer = true,
                ValidIssuer = issuer,

                // Kiểm tra Audience
                ValidateAudience = true,
                ValidAudience = audience,

                // Kiểm tra thời gian hết hạn
                ValidateLifetime = true,

                // Kiểm tra chữ ký JWT
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(secretKey)
                    ),

                // Không cho phép lệch thời gian
                ClockSkew = TimeSpan.Zero
            };
    });


//
// ============================================================
// 9. AUTHORIZATION
// ============================================================
//

// MVP chỉ cần authentication.
// Khi có chức năng Admin thì có thể dùng policy này.

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
    {
        policy.RequireRole("Admin");
    });
});


//
// ============================================================
// 10. BUILD APPLICATION
// ============================================================
//

var app = builder.Build();


//
// ============================================================
// 11. SWAGGER
// ============================================================
//

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


//
// ============================================================
// 12. MIDDLEWARE PIPELINE
// ============================================================
//

// Nếu dùng HTTPS ở môi trường production thì bật lại.
// app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();


//
// ============================================================
// 13. RUN
// ============================================================
//

app.Run();