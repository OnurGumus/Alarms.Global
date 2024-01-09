module HolidayTracker.Scheduler.API

open NodaTime
open Command.Actor
open Microsoft.Extensions.Configuration
open Akka.Streams
open Akkling.Streams
open Akkling
open Akka.Streams.Dsl

[<Interface>]
type IAPI =
    abstract Start: unit -> unit
    abstract Stop: unit -> unit

let api (env: _) (clock: IClock) (actorAPI: IActor) =
    let config = env :> IConfiguration

    { new IAPI with
        member this.Start() =
            let schedulerActor = Scheduler.Scheduler.aref actorAPI.System config
            let eventResolver = Scheduler.EventResolver.aref actorAPI.System config
            let sourceQueue:Source<System.DateTime,_> = Source.queue OverflowStrategy.DropNew 1000
            //let source = sourceQueue.MapMaterializedValue(fun queu -> schedulerActor queu)
            let qu =
                async {
                    return
                        sourceQueue
                        |> Source.toMat (Sink.forEach (fun x -> eventResolver <! x)) Keep.left
                        |> Graph.run actorAPI.Materializer
                }
                |> Async.RunSynchronously

            let ac = schedulerActor qu
            ac |> ignore

        //  |//> Source.mapMaterializedValue( fun x -> x )
        // Scheduler.Scheduler.aref actorAPI.System config sourceQueue
        // sourceQueue
        member this.Stop() = () }
