Feature: Subscribe
    
    In order to get notified about the public holidays beforehand, x days
    I would like to be able to Subscribe by selecting one more countries as well unsubscribe to them.

    # Scenario: View subscriptions
    #     Given I have some subscriptions
    #     When I view my subscriptions
    #     Then my subscriptions should visible

    Scenario: Subscribe when not authenticated
        Given I am not authenticated
        When I try to select a country
        Then system should require me to login

    Scenario: Subscribe when authenticated
        Given I am authenticated
        And I am not subscribed to a country
        When I try to select a country
        Then I should be subscribed to that country

    # Scenario: Unsubscribe
    #     Given I am subscribed to a country
    #     When I unsubscribe
    #     Then I should be unsubscribed

    # Scenario: Send mail
    #     Given I am subscribed to a country
    #     And there is vacation within a subscribed country within 7 days
    #     And If I haven't received a notification for that vacation
    #     When the mail sending time comes
    #     Then an email should be sent about the vacation for that country