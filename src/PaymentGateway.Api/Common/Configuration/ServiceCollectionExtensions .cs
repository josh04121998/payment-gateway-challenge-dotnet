using PaymentGateway.Api.Features.Payments.Services;

namespace PaymentGateway.Api.Common.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPaymentsFeatureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient("BankApi", client =>
            {
                client.BaseAddress = new Uri(configuration["BankApi:BaseAddress"]);
            });

            services.AddScoped<IAcquiringBankService, BankService>();
            services.AddSingleton<IPaymentsRepository, PaymentsRepository>();

            return services;
        }
    }
}