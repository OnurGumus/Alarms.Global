Feature: Publish events
  
    Scenario: Publish an event
    Given today is 13-MAY-2023
    And we have the following subscribers
   | Email    | Regions |
   | onur@outlook.com.tr      | Argentina               | 
   | onurgumus@hotmail.com    | Argentina,Brazil        |

    When I publish an event for Argentina 24-MAY-2023
    Then nothing should happen
    When date becomes 17-MAY-2023
    Then onur@outlook.com.tr should get a notification
    When date becomes 18-MAY-2023
    Then nothing should happen
        