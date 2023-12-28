Feature: Subscribe
    
    In order to get notified about the public holidays beforehand, x days
    I would like to be able to Subscribe by selecting one more countries as well unsubscribe to them.

    Scenario: Subscribe when not authenticated
        Given I am not authenticated
        When I try to select a region
        Then system should require me to login

    Scenario: Subscribe when authenticated
        Given I am authenticated
        And I am not subscribed to a region
        When I try to select a region
        Then I should be subscribed to that region

    Scenario: Unsubscribe
        Given I am authenticated
        And I am subscribed to a region
        When I unsubscribe from that region
        Then I should be unsubscribed from that region