Feature: Regression - Search depth validations
  Provides coverage for empty states, pagination, and sorting behaviours.

  Background:
    Given I open the Northumbria NHS homepage
    And I accept cookies if prompted

  @Regression
  Scenario: Empty search state surfaces guidance
    When I search for "northumbria-no-results-check"
    Then a no results message should be displayed

  @Regression
  Scenario: Pagination controls navigate between result pages
    When I search for "health"
    And I go to the "next" page of results
    Then the search page indicator should update after pagination
    When I go to the "previous" page of results
    Then the search page indicator should update after pagination