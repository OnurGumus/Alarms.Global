module HolidayCountry.Client.App

open Elmish
open Lit
open Lit.Elmish
open Browser
open Elmish.UrlParser
open Elmish.Navigation
open HolidayTracker.Client

CountriesSelector.register()
let doNothing () = ()