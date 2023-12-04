# Sceduler

- users are subscribed to be notified about holidays in one or more countries
- they will receive one email per holiday per country (each holiday - separate email)
- email notification is sent just for the first day of the holiday

## Workflow

- after user registers in the app
    - emails are sent right away
    - next 6 days will be processed right away (e.g. if today is TUE, you will get notifications for WED, THU, FRI, SAT, SUN, MON)
- regular workflow for existing users
    - every day at 19h
    - holidays for 7th day from today are sent (e.g. is today is TUE, you will get emails for holidays on next TUE)

## Implementation

- will not use actors, will operate directly on a database
- TODO: find good scheduler component

1. pull all countries with holidays starting on a given date
    (country1, holiday1, date)
    (country1, holiday2, date)
    (country2, holiday3, date)
2. find users subscribed to these countries
3. send one email per (userX, countryX) tuple

### Database

- Sql Lite
- Table: Users
- Table: Countries
- Table: UsersCountries
- Table: Holidays

## Future

- instead of 7 days, introduce configurable "lookahead" period
