using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using SqlSugar;
using StackExchange.Redis;
using StreamCore.Filter;
using StreamCore.Method;
using StreamCore.Service;

var builder = WebApplication.CreateBuilder(args);

// 1. 添加配置文件（确保 appsettings.json 加载）
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// 2. 添加控制器和服务层
builder.Services.AddControllers();
builder.Services.AddScoped<ShippingService>();
builder.Services.AddScoped<ProcureService>();
builder.Services.AddScoped<UploadService>();
builder.Services.AddScoped<LoadAndUnloadBox>();
builder.Services.AddScoped<SystemService>();
// 注册过滤器
builder.Services.AddScoped<AuthonizationFilter>();
//添加MVC服务
builder.Services.AddControllersWithViews(options =>
{
    // 注册全局过滤器
    options.Filters.Add<AuthonizationFilter>();
});

// 3. 添加 Swagger 服务（必须）
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("运数据监控接口测试", new OpenApiInfo
    {
        Title = "海运数据监控接口",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

// 4. 修改数据库服务注册方式（使用 Keyed Services）
// 注册 downstream 数据库
builder.Services.AddKeyedScoped<ISqlSugarClient>("downstream", (provider, key) =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("MySQL");

    return new SqlSugarClient(new ConnectionConfig
    {
        ConfigId = "downstream",
        DbType = DbType.MySql,
        ConnectionString = connectionString,
        IsAutoCloseConnection = true,
        InitKeyType = InitKeyType.Attribute
    });
});

// 注册 stream 数据库
builder.Services.AddKeyedScoped<ISqlSugarClient>("stream", (provider, key) =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("StreamMySQL");

    return new SqlSugarClient(new ConnectionConfig
    {
        ConfigId = "stream",
        DbType = DbType.MySql,
        ConnectionString = connectionString,
        IsAutoCloseConnection = true,
        InitKeyType = InitKeyType.Attribute
    });
});

// 5. 添加 Redis 连接服务
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(redisConnectionString);
});

var app = builder.Build();

// 7. 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/运数据监控接口测试/swagger.json", "海运数据监控接口测试");
    });
}

// 8. 使用路由
app.UseRouting();

// 9. 映射控制器
app.MapControllers();

// 10. 根路径测试
app.MapGet("/", () => "Hello World!");

app.Run();