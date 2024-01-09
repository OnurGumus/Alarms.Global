module Scheduler.Scheduler

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

type SchedulerMsg =
    | SchedulerTicked
    | SchedulerRequested

let aref system (config: IConfiguration) (queue: ISourceQueue<DateTime>) =
    spawn system "scheduler"
    <| props (fun m ->
        let rec loop (schedule: ICancelable option) =
            actor {
                let! (msg: obj) = m.Receive()
                let log: ILoggingAdapter = m.UntypedContext.GetLogger()

                match msg with
                | LifecycleEvent e ->
                    match e with
                    | PreStart ->
                        m.Self <! SchedulerRequested
                        return loop (schedule)

                    | PostStop ->
                        schedule.Value.Cancel()

                    | _ -> return! loop (schedule)
                | :? SchedulerMsg as msg ->
                    match msg with
                    | SchedulerTicked ->
                        let now = m.System.Scheduler.Now.UtcDateTime
                        queue.AsyncOffer(now) |> Async.Ignore |> Async.RunSynchronously
                        m.Self <! SchedulerRequested
                        return! loop (schedule)

                    | SchedulerRequested ->
                        let scheduleTime = config["config:schedule-time"] |> int
                        let now = m.System.Scheduler.Now.UtcDateTime
                        let remainingTime = TimeSpan.FromHours(scheduleTime) - now.TimeOfDay

                        let remainingTime =
                            if remainingTime >= TimeSpan.Zero then
                                remainingTime
                            else
                                remainingTime + TimeSpan.FromHours(24)

                        let c = m.Schedule remainingTime m.Self SchedulerTicked
                        log.Debug("Actor %A has started", m.Self)
                        return! loop (Some c)



                | _ -> return Unhandled

                return! loop (schedule)
            }

        loop (None))
