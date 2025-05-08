using PaymentGateway.Api.Features.Payments.Models.Responses;

namespace PaymentGateway.Api.Features.Payments.Services;

public interface IPaymentsRepository
{
    void Add(PaymentResponse payment);
    PaymentResponse Get(Guid id);
}

public class PaymentsRepository: IPaymentsRepository
{
    public List<PaymentResponse> Payments = new();
    
    public void Add(PaymentResponse postPayment)
    {
        Payments.Add(postPayment);
    }

    public PaymentResponse Get(Guid id)
    {
        return Payments.FirstOrDefault(p => p.Id == id);
    }
}