Feature: Simple LocalStack Verification
As a developer testing AWS integration
I want to verify LocalStack container works
So that I can trust the infrastructure for other tests

    @Infrastructure @LocalStack @Smoke
    Scenario: Start LocalStack and verify basic functionality
        When I start LocalStack container
        Then LocalStack should be running
        And LocalStack health check should pass
        And I should be able to create a test parameter
        And I should be able to read the test parameter back