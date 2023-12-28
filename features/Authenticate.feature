Feature: Authentication

  Scenario: Sign in
    Given I am not authenticated
    When I sign in
    Then I should be signed in

  Scenario: Sign Out
    Given I am signed in
    When I sign out
    Then I should be signed out
