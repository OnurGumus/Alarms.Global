Feature: Authentication

  # Scenario: Sign In with non OAUTH Email
  #   Given I am not Sign In
  #   When I an provide a valid email
  #   And I provide valid validation code
  #   Then I should be able to login

  # Scenario: Sign In with OAUTH Email
  #   Given I am not Sign In
  #   When I sign in with OAUTH
  #   Then I should be able to login

  Scenario: Sign in
    Given I am not authenticated
    When I sign in
    Then I should be signed in

  # Scenario: Sign Out
  #   Given I am Signed In
  #   When I sign out
  #   Then I should be signed out
