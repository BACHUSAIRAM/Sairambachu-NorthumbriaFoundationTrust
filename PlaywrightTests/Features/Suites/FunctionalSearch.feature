Feature: Functional - Site search
  As a Northumbria foundation user
  I want to search the public website and see results

  @NHS-101 @Functional
  Scenario Outline: Functional - Search returns results
    Given I open the Northumbria NHS homepage
    And I accept cookies if prompted
    When I search for "<term>"
    Then results are returned

    Examples:
      | term         |
      | covid        |
      | appointments |
      | health       |

  @NHS-101 @Functional @Negative
  Scenario: Functional (negative) - Empty search does not execute
    Given I open the Northumbria NHS homepage
    And I accept cookies if prompted
    When I search for ""
    Then the search should not navigate away from the homepage
