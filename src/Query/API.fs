﻿module HolidayTracker.Query.API

open Microsoft.Extensions.Configuration
open HolidayTracker.Shared.Model
open Subscription
open Thoth.Json.Net
open HolidayTracker.ServerInterfaces.Query
open Projection
open Akka.Streams.Dsl
open Akka.Persistence.Query
open Akka.Streams
open Akkling.Streams

[<Interface>]
type IAPI =
    abstract Query<'t> :
        ?filter: Predicate *
        ?orderby: string *
        ?orderbydesc: string *
        ?thenby: string *
        ?thenbydesc: string *
        ?take: int *
        ?skip: int ->
            list<'t> Async

    abstract Subscribe: (DataEvent -> unit) -> IKillSwitch
    abstract Subscribe: (DataEvent -> bool) * int * (DataEvent -> unit) -> IKillSwitch * Async<unit>

let subscribeToStream source mat (sink: Sink<DataEvent, _>) =
    source
    |> Source.viaMat KillSwitch.single Keep.right
    |> Source.toMat (sink) Keep.both
    |> Graph.run mat

open FSharp.Data.Sql.Common
open Serilog
open System.Linq
open HolidayTracker.Shared
open HolidayTracker.Shared.Model
open HolidayTracker.Shared.Model.Authentication

let api (config: IConfiguration) actorApi =

    let connString = config.GetSection(Constants.ConnectionString).Value

    let source = Projection.init connString actorApi

    subscribeToStream
        source
        actorApi.Materializer
        (Sink.ForEach(fun x -> Serilog.Log.Verbose("data event : {@dataevent}", x)))
    |> ignore

    let subscribeCmd =
        (fun (cb: DataEvent -> unit) ->
            let sink = Sink.forEach (fun event -> cb (event))
            let ks, _ = subscribeToStream source actorApi.Materializer sink
            ks :> IKillSwitch)

    let subscribeCmdWithFilter =
        (fun filter take (cb: DataEvent -> unit) ->
            let subscribeToStream source filter take mat (sink: Sink<DataEvent, _>) =
                source
                |> Source.viaMat KillSwitch.single Keep.right
                |> Source.filter filter
                |> Source.take take
                |> Source.toMat (sink) Keep.both
                |> Graph.run mat

            let sink = Sink.forEach (fun event -> cb (event))
            let ks, d = subscribeToStream source filter take actorApi.Materializer sink
            let d = d |> Async.Ignore
            ks :> IKillSwitch, d)

    { new IAPI with
        override this.Subscribe(cb) = subscribeCmd (cb)
        override this.Subscribe(filter, take, cb) = subscribeCmdWithFilter filter take cb

        override this.Query(?filter, ?orderby, ?orderbydesc, ?thenby, ?thenbydesc, ?take, ?skip) : Async<'t list> =
            let ctx = Sql.GetDataContext(connString)

            let rec eval2 (t) =
                match t with
                | Equal(s, n) -> <@@ fun (x: SqlEntity) -> x.GetColumn(s) = n @@>
                | NotEqual(s, n) -> <@@ fun (x: SqlEntity) -> x.GetColumn(s) <> n @@>
                | Greater(s, n) -> <@@ fun (x: SqlEntity) -> x.GetColumn(s) > n @@>
                | GreaterOrEqual(s, n) -> <@@ fun (x: SqlEntity) -> x.GetColumn(s) >= n @@>
                | Smaller(s, n) -> <@@ fun (x: SqlEntity) -> x.GetColumn(s) < n @@>
                | SmallerOrEqual(s, n) -> <@@ fun (x: SqlEntity) -> x.GetColumn(s) <= n @@>
                | And(t1, t2) -> <@@ fun (x: SqlEntity) -> (%%eval2 t1) x && (%%eval2 t2) x @@>
                | Or(t1, t2) -> <@@ fun (x: SqlEntity) -> (%%eval2 t1) x || (%%eval2 t2) x @@>
                | Not(t0) -> <@@ fun (x: SqlEntity) -> not ((%%eval2 t0) x) @@>

            let sortByEval column =
                <@@ fun (x: SqlEntity) -> x.GetColumn<System.IComparable>(column) @@>

            let augment db =
                let db =
                    match filter with
                    | Some filter ->
                        <@
                            query {
                                for c in (%db) do
                                    where ((%%eval2 filter) c)
                                    select c
                            }
                        @>
                    | None -> db

                let db =
                    match orderby with
                    | Some orderby ->
                        <@
                            query {
                                for c in (%db) do
                                    sortBy ((%%sortByEval orderby) c)
                                    select c
                            }
                        @>
                    | None ->
                        <@
                            query {
                                for c in (%db) do
                                    select c
                            }
                        @>

                let db =
                    match orderbydesc with
                    | Some orderbydesc ->
                        <@
                            query {
                                for c in (%db) do
                                    sortByDescending ((%%sortByEval orderbydesc) c)
                                    select c
                            }
                        @>
                    | None -> db

                let db =
                    match thenby with
                    | Some thenby ->
                        <@
                            query {
                                for c in (%db) do
                                    thenBy ((%%sortByEval thenby) c)
                                    select c
                            }
                        @>
                    | None -> db

                let db =
                    match thenbydesc with
                    | Some thenbydesc ->
                        <@
                            query {
                                for c in (%db) do
                                    thenByDescending ((%%sortByEval thenbydesc) c)
                                    select c
                            }
                        @>
                    | None -> db

                let db =
                    match take with
                    | Some take -> <@ (%db).Take(take) @>
                    | None -> db

                let db =
                    match skip with
                    | Some skip -> <@ (%db).Skip(skip) @>
                    | None -> db

                query {
                    for u in (%db) do
                        select u
                }

            let res =
                if typeof<'t> = typeof<Region> then
                    let q =
                        query {
                            for c in ctx.Main.Regions do
                                select c
                        }

                    augment <@ q @>
                    |> Seq.map (fun x ->
                        { RegionId = (ShortString.TryCreate x.RegionId) |> forceValidate |> RegionId
                          Name = x.Name |> ShortString.TryCreate |> forceValidate
                          AlrernateNames = x.AlternateNames |> decode |> forceValidateWithString
                          RegionType =
                            match x.Type with
                            | "Country" -> Country
                            | _ -> Country }
                        : Region)
                    |> List.ofSeq
                    |> box

                elif typeof<'t> = typeof<User> then
                    let q =
                        query {
                            for c in ctx.Main.UserIdentities do
                                select c
                        }

                    augment <@ q @>
                    |> Seq.map (fun x ->
                        { ClientId = x.ClientId |> UserClientId.TryCreate |> forceValidate
                          Identity = x.Identity |> UserIdentity.Create
                          Version = x.Version |> Version }
                        : User)
                    |> List.ofSeq
                    |> box
                elif typeof<'t> = typeof<UserSubscription> then
                    let q =
                        query {
                            for c in ctx.Main.Subscriptions do
                                select c
                        }

                    augment <@ q @>
                    |> Seq.map (fun x ->
                        { Identity = x.Identity |> UserIdentity.Create
                          RegionId = x.RegionId |> RegionId.Create }
                        : UserSubscription)
                    |> List.ofSeq
                    |> box
                elif typeof<'t> = typeof<UserSetting> then
                    let q =
                        query {
                            for c in ctx.Main.UserSettings do
                                select c
                        }

                    augment <@ q @>
                    |> Seq.map (fun x ->
                        { Identity = x.Identity |> UserIdentity.Create
                          BeforeDays = x.ReminderDays |> int }
                        : UserSetting)
                    |> List.ofSeq
                    |> box

                elif typeof<'t> = typeof<GlobalEvent> then
                    let q =
                        query {
                            for c in ctx.Main.GlobalEvents do
                                select c
                        }

                    augment <@ q @>
                    |> Seq.map (fun x ->
                        { GlobalEventId = x.Id |> GlobalEventId.Create
                          Title = x.Title |> ShortString.TryCreate |> forceValidate
                          Body = x.Body |> LongString.TryCreate |> forceValidate
                          TargetRegion =
                            query {
                                for er in ctx.Main.EventsRegions do
                                    where (er.EventId = x.Id)
                                    select er.RegionId
                            }
                            |> Seq.map (fun r -> RegionId.Create r)
                            |> List.ofSeq
                          EventDateInUTC = x.TargetDate |> System.DateTime.Parse |> Some }
                        : GlobalEvent)
                    |> List.ofSeq
                    |> box
                else
                    failwith "not implemented"

            async { return res :?> list<'t> } }
