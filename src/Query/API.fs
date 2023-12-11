﻿module HolidayTracker.Query.API

open Microsoft.Extensions.Configuration
open HolidayTracker.Shared.Model
open Thoth.Json.Net
open HolidayTracker.ServerInterfaces.Query
open Projection

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


open FSharp.Data.Sql.Common
open Serilog
open System.Linq
open HolidayTracker.Shared
open HolidayTracker.Shared.Model
open HolidayTracker.Shared.Model.Authentication

let api (config: IConfiguration) actorApi =

    let connString = config.GetSection(Constants.ConnectionString).Value

    { new IAPI with

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
                    let credit =
                        query {
                            for c in ctx.Main.Regions do
                                select c
                        }

                    augment <@ credit @>
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
                else
                    failwith "not implemented"

            async { return res :?> list<'t> } }