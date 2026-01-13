Feature: Data-driven - Search coverage via external datasets
  Executes search journeys using keywords sourced from JSON and CSV files.

  @DDT @Search
  Scenario: Execute JSON-backed search cases
    Given I load the search dataset "smoke" from "Data/search-datasets.json"
    When I execute the loaded data-driven search cases
    Then each data-driven search should satisfy expectations

  @DDT @Search
  Scenario: Execute CSV-backed search cases
    Given I load the search dataset "empty" from "Data/search-datasets.csv"
    When I execute the loaded data-driven search cases
    Then each data-driven search should satisfy expectations
