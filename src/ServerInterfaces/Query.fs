module HolidayTracker.ServerInterfaces.Query
open HolidayTracker.Shared.Model

[<Interface>]
type IQuery =
    abstract Query<'t> : ?filter:Predicate * ?orderby:string * ?orderbydesc:string * ?thenby:string  * ?thenbydesc:string * ?take:int * ?skip:int -> list<'t> Async