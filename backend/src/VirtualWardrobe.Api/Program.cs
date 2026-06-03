using VirtualWardrobe.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiHosting();

var app = builder.Build();
app.UseApiHosting();

app.Run();

public partial class Program;
