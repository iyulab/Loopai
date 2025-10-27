using Loopai.Client;

var builder = WebApplication.CreateBuilder(args);

// Add Loopai client
builder.Services.AddLoopaiClient(options =>
{
    options.BaseUrl = builder.Configuration["Loopai:BaseUrl"] ?? "http://localhost:8080";
    options.ApiKey = builder.Configuration["Loopai:ApiKey"];
    options.Timeout = TimeSpan.FromSeconds(60);
    options.MaxRetries = 3;
    options.EnableDetailedLogging = builder.Environment.IsDevelopment();
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
