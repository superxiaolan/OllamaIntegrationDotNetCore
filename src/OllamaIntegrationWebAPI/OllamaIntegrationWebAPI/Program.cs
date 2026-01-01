using OllamaSharp;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 读取配置
var ollamaUrl = builder.Configuration["OllamaConfig:BaseUrl"];
var modelName = builder.Configuration["OllamaConfig:ModelName"];

// 注册 Ollama 客户端
builder.Services.AddSingleton<IOllamaApiClient>(new OllamaApiClient(ollamaUrl, modelName));

// 配置 CORS 以允许来自 Vue 应用的请求
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp",
        builder => builder.WithOrigins("http://localhost:5173") // Vue 的默认地址
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowVueApp"); // 必须在 UseAuthorization 之前调用

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
