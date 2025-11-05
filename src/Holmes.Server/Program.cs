var builder = WebApplication.CreateBuilder(args);

// TODO: register core services, persistence, and modules.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok("Holmes server online"))
   .WithName("GetRoot")
   .WithOpenApi();

app.Run();
