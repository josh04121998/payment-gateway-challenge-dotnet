using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using FluentValidation;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using PaymentGateway.Api.Common.Configuration;
using PaymentGateway.Api.Features.Payments;
using PaymentGateway.Api.Features.Payments.Enums;
using PaymentGateway.Api.Features.Payments.Handler;
using PaymentGateway.Api.Features.Payments.Mappers;
using PaymentGateway.Api.Features.Payments.Models.BankService;
using PaymentGateway.Api.Features.Payments.Models.Requests;
using PaymentGateway.Api.Features.Payments.Models.Responses;
using PaymentGateway.Api.Features.Payments.Services;

namespace PaymentGateway.Api.Tests
{
    public class PaymentsControllerTests : IDisposable
    {
        private readonly HttpClient _apiClient;
        private readonly Mock<ILogger<PaymentHandler>> _paymentServiceLoggerMock;
        private readonly Mock<IAcquiringBankService> _acquiringBankServiceMock;
        private readonly Mock<IPaymentsRepository> _paymentsRepositoryMock;
        private readonly IValidator<PostPaymentRequest> _validator;
        private readonly Random _random;
        private readonly PaymentHandler _paymentService;

        public PaymentsControllerTests()
        {
            _paymentServiceLoggerMock = new Mock<ILogger<PaymentHandler>>();
            _acquiringBankServiceMock = new Mock<IAcquiringBankService>();
            _paymentsRepositoryMock = new Mock<IPaymentsRepository>();
            _validator = new PaymentRequestValidator();
            _random = new Random();

            var paymentMapper = new PaymentMapper();
            _paymentService = new PaymentHandler(
                _acquiringBankServiceMock.Object,
                _paymentsRepositoryMock.Object,
                paymentMapper,
                _paymentServiceLoggerMock.Object);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "BankApi:BaseAddress", "https://api.bank.com" }
                })
                .Build();

            var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
            _apiClient = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(_validator);
                    services.AddSingleton(_paymentService);
                    services.AddPaymentsFeatureServices(configuration);
                    services.AddSingleton(_paymentsRepositoryMock.Object);
                    services.AddSingleton(_acquiringBankServiceMock.Object);
                }))
                .CreateClient();
        }

        public void Dispose()
        {
            _apiClient.Dispose();
        }

        private static PostPaymentRequest CreateValidPaymentRequest()
        {
            var expiryDate = DateTime.Today.AddYears(2).AddMonths(2);
            return new PostPaymentRequest
            {
                CardNumber = "2222405343248877",
                ExpiryMonth = expiryDate.Month,
                ExpiryYear = expiryDate.Year,
                Currency = "GBP",
                Amount = 1500,
                Cvv = "123"
            };
        }

        private PaymentResponse CreateRandomPaymentResponse()
        {
            return new PaymentResponse
            {
                Id = Guid.NewGuid(),
                ExpiryYear = _random.Next(2023, 2030),
                ExpiryMonth = _random.Next(1, 12),
                Amount = _random.Next(1, 10000),
                CardNumberLastFour = _random.Next(1111, 9999).ToString(),
                Currency = "GBP"
            };
        }

        [Fact]
        public async Task GetPaymentById_WhenPaymentExists_Returns200WithPayment()
        {
            // Arrange
            var payment = CreateRandomPaymentResponse();
            _paymentsRepositoryMock.Setup(r => r.Get(payment.Id)).Returns(payment);

            // Act
            var response = await _apiClient.GetAsync($"/api/Payments/{payment.Id}");
            var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            paymentResponse.Should().NotBeNull();
            paymentResponse.Should().BeEquivalentTo(payment);
        }

        [Fact]
        public async Task GetPaymentById_WhenPaymentNotFound_Returns404()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            _paymentsRepositoryMock.Setup(r => r.Get(paymentId)).Returns((PaymentResponse)null);

            // Act
            var response = await _apiClient.GetAsync($"/api/Payments/{paymentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            _paymentServiceLoggerMock.VerifyLog(LogLevel.Warning, $"Payment not found, PaymentId: {paymentId}", Times.Once());
        }

        [Theory]
        [InlineData("5555555555554444", 1, 2030, "123", 25000, "GBP", "4444")]
        [InlineData("5105105105105100", 2, 2031, "002", 100, "GBP", "5100")]
        [InlineData("4111111111111111", 3, 2032, "5000", 15000, "EUR", "1111")]
        [InlineData("4012888888881881", 4, 2033, "1234", 350000, "EUR", "1881")]
        [InlineData("42222222222222", 5, 2034, "5555", 1234565, "USD", "2222")]
        [InlineData("3530111333300000", 6, 2035, "333", 65656565, "USD", "0000")]
        [InlineData("378282246310005", 7, 2036, "000", 12345678, "GBP", "0005")]
        [InlineData("378734493671000", 8, 2038, "010", 55555555, "EUR", "1000")]
        [InlineData("5610591081018250", 9, 2039, "010", 2222222, "USD", "8250")]
        [InlineData("6011111111111117", 10, 2040, "010", 111111, "GBP", "1117")]
        [InlineData("6011000990139424", 11, 2041, "010", 150000, "EUR", "9424")]
        [InlineData("3530111333300000", 12, 2042, "010", 25000, "USD", "0000")]
        public async Task PostPayment_WhenAuthorized_Returns201WithPaymentDetails(
            string cardNumber, int expiryMonth, int expiryYear, string cvv, int amount, string currency, string expectedLast4Digits)
        {
            // Arrange
            var paymentRequest = new PostPaymentRequest
            {
                CardNumber = cardNumber,
                ExpiryMonth = expiryMonth,
                ExpiryYear = expiryYear,
                Cvv = cvv,
                Amount = amount,
                Currency = currency
            };

            var bankResponse = new BankPaymentResponse
            {
                Authorized = true,
                AuthorizationCode = Guid.NewGuid().ToString()
            };

            var paymentResponse = new PaymentResponse
            {
                Id = Guid.NewGuid(),
                Amount = paymentRequest.Amount,
                CardNumberLastFour = expectedLast4Digits,
                Currency = paymentRequest.Currency,
                ExpiryMonth = paymentRequest.ExpiryMonth,
                ExpiryYear = paymentRequest.ExpiryYear,
                Status = PaymentStatus.Authorized
            };

            _acquiringBankServiceMock
                .Setup(s => s.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
                .ReturnsAsync(bankResponse);

            _paymentsRepositoryMock
                .Setup(r => r.Add(It.IsAny<PaymentResponse>()))
                .Callback<PaymentResponse>(p =>
                {
                    paymentResponse.Id = p.Id; // Capture the generated ID
                    _paymentsRepositoryMock.Setup(r => r.Get(p.Id)).Returns(paymentResponse);
                });

            // Act
            var response = await _apiClient.PostAsJsonAsync("/api/Payments", paymentRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var returnedPaymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
            returnedPaymentResponse.Should().NotBeNull();
            returnedPaymentResponse.Status.Should().Be(PaymentStatus.Authorized);
            returnedPaymentResponse.Id.Should().NotBe(Guid.Empty);
            returnedPaymentResponse.CardNumberLastFour.Should().Be(expectedLast4Digits);
            returnedPaymentResponse.ExpiryMonth.Should().Be(expiryMonth);
            returnedPaymentResponse.ExpiryYear.Should().Be(expiryYear);
            returnedPaymentResponse.Currency.Should().Be(currency);
            returnedPaymentResponse.Amount.Should().Be(amount);

            var repoPayment = _paymentsRepositoryMock.Object.Get(paymentResponse.Id);
            returnedPaymentResponse.Should().BeEquivalentTo(repoPayment);

            _paymentServiceLoggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().StartsWith("Payment processed successfully") &&
                                                  v.ToString().Contains($"PaymentId: {paymentResponse.Id}") &&
                                                  v.ToString().Contains($"Status: {PaymentStatus.Authorized}") &&
                                                  v.ToString().Contains($"CardNumberLastFour: {expectedLast4Digits}") &&
                                                  v.ToString().Contains($"Amount: {amount} {currency}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once());
        }

        [Theory]
        [InlineData("5445", 1, 2030, "123", 25000, "GBP", "Small card number")]
        [InlineData("54458927498324982734928374928743", 1, 2030, "123", 25000, "GBP", "Large card number")]
        [InlineData("", 1, 2030, "123", 25000, "GBP", "Empty card number")]
        [InlineData("5555555555554444", 0, 2030, "123", 25000, "GBP", "Invalid month (zero)")]
        [InlineData("5555555555554444", -1, 2030, "123", 25000, "GBP", "Invalid month (negative)")]
        [InlineData("5555555555554444", 13, 2030, "123", 25000, "GBP", "Invalid month (too high)")]
        [InlineData("5555555555554444", 1, 2019, "123", 25000, "GBP", "Invalid year")]
        [InlineData("5555555555554444", 1, 2030, "12", 25000, "GBP", "Small CVV")]
        [InlineData("5555555555554444", 1, 2030, "12345", 25000, "GBP", "Large CVV")]
        [InlineData("5555555555554444", 1, 2030, "123", 0, "GBP", "Zero amount")]
        [InlineData("5555555555554444", 1, 2030, "123", -200, "GBP", "Negative amount")]
        [InlineData("5555555555554444", 1, 2030, "123", 25000, "GB", "Invalid currency")]
        [InlineData("5555555555554444", 1, 2030, "123", 25000, "", "Empty currency")]
        [InlineData("5555555555554444", 1, 2030, "123", 25000, "DKK", "Unsupported currency")]
        [InlineData("5445", -1, 1999, "123456", -100, "DKK", "Multiple violations")]
        public async Task PostPayment_WhenInvalidParameters_Returns400WithValidationErrors(
            string cardNumber, int expiryMonth, int expiryYear, string cvv, int amount, string currency, string scenario)
        {
            // Arrange
            var paymentRequest = new PostPaymentRequest
            {
                CardNumber = cardNumber,
                ExpiryMonth = expiryMonth,
                ExpiryYear = expiryYear,
                Cvv = cvv,
                Amount = amount,
                Currency = currency
            };

            // Act
            var response = await _apiClient.PostAsJsonAsync("/api/Payments", paymentRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var validationResponse = await response.Content.ReadFromJsonAsync<PostPaymentFailedValidationResponse>();
            validationResponse.Should().NotBeNull();
            validationResponse.Status.Should().Be(PaymentStatus.Rejected);
            validationResponse.Errors.Should().NotBeEmpty();

            _acquiringBankServiceMock.Verify(s => s.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()), Times.Never());
        }

        [Fact]
        public async Task PostPayment_WhenPastExpirationDate_Returns400WithValidationErrors()
        {
            // Arrange
            var paymentRequest = CreateValidPaymentRequest();
            var expiryDate = DateTime.Today.AddYears(-1);
            paymentRequest.ExpiryMonth = expiryDate.Month;
            paymentRequest.ExpiryYear = expiryDate.Year;

            // Act
            var response = await _apiClient.PostAsJsonAsync("/api/Payments", paymentRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var validationResponse = await response.Content.ReadFromJsonAsync<PostPaymentFailedValidationResponse>();
            validationResponse.Should().NotBeNull();
            validationResponse.Status.Should().Be(PaymentStatus.Rejected);
            validationResponse.Errors.Should().NotBeEmpty();

            if (paymentRequest.ExpiryYear == DateTime.Today.Year)
                validationResponse.Errors.Should().Contain(e => e.PropertyName == "ExpiryMonth/ExpiryYear");
            else
                validationResponse.Errors.Should().Contain(e => e.PropertyName == "ExpiryYear");

            _acquiringBankServiceMock.Verify(s => s.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()), Times.Never());
        }

        [Fact]
        public async Task PostPayment_WhenDeclined_Returns201WithDeclinedStatus()
        {
            // Arrange
            var paymentRequest = CreateValidPaymentRequest();
            var bankResponse = new BankPaymentResponse { Authorized = false, AuthorizationCode = "" };

            var paymentResponse = new PaymentResponse
            {
                Id = Guid.NewGuid(),
                Amount = paymentRequest.Amount,
                CardNumberLastFour = "8877",
                Currency = paymentRequest.Currency,
                ExpiryMonth = paymentRequest.ExpiryMonth,
                ExpiryYear = paymentRequest.ExpiryYear,
                Status = PaymentStatus.Declined
            };

            _acquiringBankServiceMock
                .Setup(s => s.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
                .ReturnsAsync(bankResponse);

            _paymentsRepositoryMock
                .Setup(r => r.Add(It.IsAny<PaymentResponse>()))
                .Callback<PaymentResponse>(p =>
                {
                    paymentResponse.Id = p.Id; // Capture the generated ID
                    _paymentsRepositoryMock.Setup(r => r.Get(p.Id)).Returns(paymentResponse);
                });

            // Act
            var response = await _apiClient.PostAsJsonAsync("/api/Payments", paymentRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var returnedPaymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
            returnedPaymentResponse.Should().NotBeNull();
            returnedPaymentResponse.Status.Should().Be(PaymentStatus.Declined);
            returnedPaymentResponse.Id.Should().NotBe(Guid.Empty);
            returnedPaymentResponse.CardNumberLastFour.Should().Be("8877");
            returnedPaymentResponse.ExpiryMonth.Should().Be(paymentRequest.ExpiryMonth);
            returnedPaymentResponse.ExpiryYear.Should().Be(paymentRequest.ExpiryYear);
            returnedPaymentResponse.Currency.Should().Be(paymentRequest.Currency);
            returnedPaymentResponse.Amount.Should().Be(paymentRequest.Amount);

            var repoPayment = _paymentsRepositoryMock.Object.Get(paymentResponse.Id);
            returnedPaymentResponse.Should().BeEquivalentTo(repoPayment);
            repoPayment.Status.Should().Be(PaymentStatus.Declined);

            _paymentServiceLoggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().StartsWith("Payment processed successfully") &&
                                                  v.ToString().Contains($"PaymentId: {paymentResponse.Id}") &&
                                                  v.ToString().Contains($"Status: {PaymentStatus.Declined}") &&
                                                  v.ToString().Contains($"CardNumberLastFour: 8877") &&
                                                  v.ToString().Contains($"Amount: {paymentRequest.Amount} {paymentRequest.Currency}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once());
        }

        [Fact]
        public async Task PostPayment_WhenBankThrowsError_Returns500()
        {
            // Arrange
            var paymentRequest = CreateValidPaymentRequest();

            _acquiringBankServiceMock
                .Setup(s => s.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
                .ThrowsAsync(new HttpRequestException("Bank API error"));

            // Act
            var response = await _apiClient.PostAsJsonAsync("/api/Payments", paymentRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            var errorMessage = await response.Content.ReadAsStringAsync();
            errorMessage.Should().Contain("Failed to communicate with the bank service.");

            _paymentServiceLoggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().StartsWith("Failed to process payment") &&
                                                  v.ToString().Contains($"CardNumberLastFour: 8877") &&
                                                  v.ToString().Contains($"Amount: {paymentRequest.Amount} {paymentRequest.Currency}") &&
                                                  v.ToString().Contains("Error: Bank API error")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once());
        }

        [Fact]
        public async Task PostPayment_WhenBankReturnsNullResponse_Returns500()
        {
            // Arrange
            var paymentRequest = CreateValidPaymentRequest();

            _acquiringBankServiceMock
                .Setup(s => s.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
                .ReturnsAsync((BankPaymentResponse)null);

            // Act
            var response = await _apiClient.PostAsJsonAsync("/api/Payments", paymentRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            var errorMessage = await response.Content.ReadAsStringAsync();
            errorMessage.Should().Contain("An error occurred while processing the payment.");

            _paymentServiceLoggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().StartsWith("Failed to process payment") &&
                                                  v.ToString().Contains($"CardNumberLastFour: 8877") &&
                                                  v.ToString().Contains($"Amount: {paymentRequest.Amount} {paymentRequest.Currency}") &&
                                                  v.ToString().Contains("Error: Bank service returned an invalid response.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once());
        }

        [Fact]
        public async Task PostPayment_WhenBankResponseDeserializationFails_Returns500()
        {
            // Arrange
            var paymentRequest = CreateValidPaymentRequest();

            _acquiringBankServiceMock
                .Setup(s => s.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
                .ThrowsAsync(new JsonException("Failed to deserialize bank response"));

            // Act
            var response = await _apiClient.PostAsJsonAsync("/api/Payments", paymentRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            var errorMessage = await response.Content.ReadAsStringAsync();
            errorMessage.Should().Contain("Invalid response format from the bank service.");

            _paymentServiceLoggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().StartsWith("Failed to process payment") &&
                                                  v.ToString().Contains($"CardNumberLastFour: 8877") &&
                                                  v.ToString().Contains($"Amount: {paymentRequest.Amount} {paymentRequest.Currency}") &&
                                                  v.ToString().Contains("Error: Failed to deserialize bank response")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once());
        }

        [Fact]
        public async Task PostPayment_WhenBankRequestTimesOut_Returns500()
        {
            // Arrange
            var paymentRequest = CreateValidPaymentRequest();

            _acquiringBankServiceMock
                .Setup(s => s.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
                .ThrowsAsync(new TaskCanceledException("Bank API request timed out"));

            // Act
            var response = await _apiClient.PostAsJsonAsync("/api/Payments", paymentRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            var errorMessage = await response.Content.ReadAsStringAsync();
            errorMessage.Should().Contain("Bank service request timed out.");

            _paymentServiceLoggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().StartsWith("Failed to process payment") &&
                                                  v.ToString().Contains($"CardNumberLastFour: 8877") &&
                                                  v.ToString().Contains($"Amount: {paymentRequest.Amount} {paymentRequest.Currency}") &&
                                                  v.ToString().Contains("Error: Bank API request timed out")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once());
        }
    }

    public static class LoggerMockExtensions
    {
        public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel logLevel, string message, Times times)
        {
            loggerMock.Verify(
                x => x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == message),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                times);
        }
    }
}