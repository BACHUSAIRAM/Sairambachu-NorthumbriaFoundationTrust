Feature: Smoke - Northumbria critical search path
  Ensures the public homepage loads quickly and the hero search works end-to-end.

  Background:
    Given I open the Northumbria NHS homepage
    And I accept cookies if prompted

  @Smoke @CriticalPath
  Scenario: Basic search returns results
    When I search for "appointments"
    Then results are returned

  @Smoke @CriticalPath
  Scenario: Homepage navigation meets SLA
    When I measure the page load performance
    Then the page load time should be less than 3 seconds
