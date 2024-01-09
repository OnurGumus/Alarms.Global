module Scheduler.EventResolver

open Akkling
open Akka.Actor
open Akka.Streams
open Akka.Streams.Dsl
open Akkling.Streams
open System
open System
open Akkling
open Akka.Actor
open Akka.Logger.Serilog
open Akka.Event
open Microsoft.Extensions.Configuration
open HolidayTracker.Query.Projection

type SchedulerTicked = SchedulerTicked

let aref system (config: IConfiguration) =
    let ctx = Sql.GetDataContext(config["config:connection-string"])
    spawn system "event-resolver"
    <| props (fun m ->
        let rec loop () =
            actor {
                let! (msg: obj) = m.Receive()
                let log: ILoggingAdapter = m.UntypedContext.GetLogger()

                match msg with
                | LifecycleEvent _ -> return! loop ()
                | :? System.DateTime as date ->
                    let dateString = date.ToString("yyyy-MM-dd HH:mm:ss")

                    let events =
                        query {
                            for e in ctx.Main.GlobalEvents do
                                for rx in ctx.Main.EventsRegions do
                                    where (e.TargetDate >= dateString && rx.EventId = e.Id)
                                    select (e, rx.RegionId)
                        }
                        |> Seq.groupBy (fst)
                        |> Seq.map (fun (k, v) -> (k, v |> Seq.map snd))

                    log.Debug("Events {@events}", events)
                | _ -> return Unhandled

                return! loop ()
            }

        loop ())
