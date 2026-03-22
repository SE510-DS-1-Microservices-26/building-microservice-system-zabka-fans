using InternshipTracker.Application.DTOs;
using InternshipTracker.Application.DTOs.Requests;
using InternshipTracker.Application.DTOs.Responses;
using InternshipTracker.Application.Enums;
using InternshipTracker.Application.Interfaces;
using InternshipTracker.Infrastructure;
using InternshipTracker.Infrastructure.Persistence;
using InternshipTracker.UI.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
await app.Services.ApplyAutomaticMigrationsAsync();
app.MapInternshipEndpoints();                                                                                              
app.MapUserEndpoints();      
app.MapApplicationEndpoints();                                                                                             
app.MapHealthEndpoints(); 

app.Run();