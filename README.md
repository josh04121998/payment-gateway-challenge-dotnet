# Instructions for candidates

This is the .NET version of the Payment Gateway challenge. If you haven't already read this [README.md](https://github.com/cko-recruitment/) on the details of this exercise, please do so now. 

## Template structure
```
src/
    PaymentGateway.Api - a skeleton ASP.NET Core Web API
test/
    PaymentGateway.Api.Tests - an empty xUnit test project
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln
```

Feel free to change the structure of the solution, use a different test library etc.

## Implementation Details and Design Choices



### Prerequisites
- .NET SDK 8.0
- Docker
- Visual Studio 2022 (optional)


### Start the Bank Simulator
Ensure that Docker is running on your machine. Open your terminal, and navigate to the project's root directory.

To start the simulator, run the following command: 
```
docker-compose up
```

### Notes
- Solution build and runs with get end point, 2 tests get works, 404 not found test currently fails
- Fix failing test when no payment found for guid return 404 
- Implement logger to warm of not found payment



### Requirements
# The product requirements for this initial phase are the following:

- A merchant should be able to process a payment through the payment gateway and receive one of the following types of response:
  - Authorized - the payment was authorized by the call to the acquiring bank
  - Declined - the payment was declined by the call to the acquiring bank
  - Rejected - No payment could be created as invalid information was supplied to the payment gateway and therefore it has rejected the request without calling the acquiring bank
- A merchant should be able to retrieve the details of a previously made payment