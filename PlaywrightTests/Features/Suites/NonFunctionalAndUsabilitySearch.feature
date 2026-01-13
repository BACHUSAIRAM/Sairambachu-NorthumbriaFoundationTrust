Feature: Non-functional & usability - Search
  Provides coverage for accessibility, keyboard/mouse usability, and performance characteristics
  of the search journey.

  Background:
    Given I open the Northumbria NHS homepage
    And I accept cookies if prompted

  @NHS-102 @Accessibility
  Scenario Outline: Accessibility - Search accessibility validations
    When I enter "<term>" into the site search and submit
    Then results are returned
    Then the page should meet screen reader accessibility requirements

    Examples:
      | term         |
      | covid        |
      | appointments |
      | health       |

  @NHS-103 @Accessibility @Keyboard @Usability
  Scenario Outline: Keyboard-only - Search via keyboard navigation
    When I navigate to the search field using the keyboard (Tab)
    And I enter "<term>" into the site search and submit using the Enter key
    Then results are returned

    Examples:
      | term         |
      | covid        |
      | appointments |
      | health       |

  @NHS-104 @Usability @Mouse
  Scenario Outline: Mouse-based - Search via mouse interactions
    When I navigate to the search field using the mouse (click)
    And I enter "<term>" into the site search and submit using the mouse
    Then results are returned
    And I should see results corresponding to the entered term
    And the search input was interactable and the submit control was clickable

    Examples:
      | term         |
      | covid        |
      | appointments |
      | health       |

  @NHS-105 @Performance @NonFunctional
  Scenario Outline: Performance - Search performance meets NHS standards
    Given I am on the Northumbria homepage
    When I measure the page load performance
    Then the page load time should be less than 3 seconds
    When I search for "<term>"
    And I measure the search response performance
    Then the search response time should be less than 2 seconds
    Then the results page should load within acceptable time
    And the page should have optimal resource loading metrics
    And the performance metrics should be logged for monitoring

    Examples:
      | term         |
      | covid        |
      | appointments |
      | health       |
      | emergency    |
      | vaccination  |

  @NHS-106 @Accessibility @Contrast @WCAG @NonFunctional
  Scenario Outline: Contrast - Color contrast meets WCAG 2.1 AA standards
    Given I am on the Northumbria homepage
    When I analyze the color contrast of the page
    Then all text elements should meet WCAG 2.1 AA contrast requirements
    And the contrast ratio for normal text should be at least 4.5:1
    And the contrast ratio for large text should be at least 3:1
    And interactive elements should have sufficient contrast
    When I search for "<term>"
    Then the search results page should meet contrast requirements
    And all result headings should have sufficient contrast
    And all result descriptions should have sufficient contrast
    And navigation links should meet contrast standards
    And the contrast analysis should be logged with detailed results

    Examples:
      | term         |
      | covid        |
      | appointments |
      | health       |
