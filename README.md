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


### Start the Bank Simulator
Ensure that Docker is running on your machine. Open your terminal, and navigate to the project's root directory.

To start the simulator, run the following command: 
```
docker-compose up
```

### FLOW
GET
request -> call db -> return response
POST
request -> validate -> callbank -> update db -> respond to request

### Notes
- Solution build and runs with get end point, 2 tests get works, 404 not found test currently fails
- Fix failing test when no payment found for guid return 404 
- Implement logger to warm of not found payment 
- Payment repo would normally be a db - we should put an inteface on this so it can be mocked/swapped with a db 
- Move on to implement the proccess payment
- Implement a mock endpoint to return 201 authorized 
- Switching post request to be similar format to call to bank eg string card number and cvv 
- validate request against required fields in read me - respond with bad request and list of error messages
    - Selected 3 currencies GBP USD EUR
    - using FluentValidation as this allows for me to seperate validation logic from api model, better control over rules, validation can be mocked if required
    - Validator as a transient to new up one per request - isolated validation logic for each request
- make call to bank
    - throughout the code I am using camel case - but the aquiring bank requires snake case using JsonPropertyName attribute to configure json field names to use snake case
    - Implemented IHttpClientFactory and a named client
        - Manage lifecycle to prevent socket exhaustion.
        - simplifies unit testing 
        - centralized config
    - Add AcquiringBankService as scoped to 
        - ensures one AcquiringBankService instance per HTTP request
        - Allows proper disposal of resources
    - Handle unsuccesful responses
- Store response in db
    - Again singleton to allow reuse of connection
- Finally write unit tests based on requirements (validation(declined), accepted, rejected)
    - Check code covergae to make sure no code is unreachable/ untested
- Refactor time

### Key consideerations 
- Easy to follow codebase - gone for feature scoped all payments related code is grouped, easy to understand/ add new feature
- Field-level validation errors: When input is invalid, the API returns 400 Bad Request and a list of individual error messages so merchants see exactly what went wrong.
- No auth at this stage endpoints are open and unsecured

### Technical Considerations
- Move orechstration of payment to PaymentService to maintain SRP
    - Error handling produces custom exception with meaningful info
    - Logging adds payment context
- Introduce PaymentMapper
    - SRP only for mapping between DTO's
    - OCP if mapping logic chnages extend mapper and not change controller
- Refactor controller
    - SRP only handle http concerns
    - Error handling catches PaymentProcessingException and returns a user-friendly error message.
    - Improve readability
- Records
    - Records are immutable once created, preventing accidental changes.  
    - Records provide value-based equality out of the box
- Custom Exceptions
    - Code throws well named exceptions
    - Improvement would be to add error handling middleware to translates exceptions into consistent HTTP responses without repetitive try/catch blocks.  
- E2E tests
    - Tests are highlevel black boc approach making them resilient to refactoring 
    - exercise the whole app from request to response
- Services are currently per feature
    - If more features require the same services this can be moved out to common or even a infrastreucture project
- More time... 
    - If I had more time i would implement correlation ids to track the end to end process 
    - end to end / integration tests could be created with a concrete bank and db (test live connections)
    





### Requirements
https://github.com/cko-recruitment/#requirements