using FluentValidation;

using PaymentGateway.Api.Common.Configuration;
using PaymentGateway.Api.Features.Payments.Handler;
using PaymentGateway.Api.Features.Payments.Mappers;
using PaymentGateway.Api.Features.Payments.Models.Requests;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register services
builder.Services.AddPaymentsFeatureServices(builder.Configuration);
builder.Services.AddScoped<PaymentHandler>();
builder.Services.AddScoped<IPaymentMapper, PaymentMapper>();
builder.Services.AddTransient<IValidator<PostPaymentRequest>, PaymentRequestValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();