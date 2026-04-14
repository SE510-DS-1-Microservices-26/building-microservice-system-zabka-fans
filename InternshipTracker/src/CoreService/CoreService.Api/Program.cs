using CoreService.Api.CoreEndpoints;
using CoreService.Api.Middleware;
using CoreService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCoreInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();

app.MapApplicationEndpoints();
app.MapInternshipEndpoints();
app.MapUserEndpoints();
app.MapHealthEndpoints();
app.Run();


public partial class Program { }