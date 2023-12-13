module Scheduler.App

open Akkling
open Akka.Streams
open Akka.Streams.Dsl
open Akkling.Streams
open System.Linq

let system = System.create "scheduler" <| Configuration.defaultConfig ()

let mat = system.Materializer()

let runStream mat =
    Enumerable.Range(1, 10)
    |> Source.From
    |> Source.runForEach mat (fun x -> printfn $"{x}")
    |> Async.RunSynchronously

let main = runStream mat
