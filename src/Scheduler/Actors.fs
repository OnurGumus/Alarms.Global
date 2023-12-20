module Scheduler.Actors

(*
#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akka.Streams"
#r "nuget: Akkling"
#r "nuget: Akkling.Streams"
*)

open Akkling
open Akka.Actor
open Akka.Streams
open Akka.Streams.Dsl
open Akkling.Streams
open System


let system = System.create "scheduler" <| Configuration.defaultConfig ()

let mat = system.Materializer()

//===================================================================================================

type Subscriber = { Name: string; Email: string }

type SendEmailNotification =
    { Subscriber: Subscriber
      EventName: string
      EmailMessage: string }

type NotificationProcessorMsg = SendEmailNotification of SendEmailNotification


type EmailSender = SendEmailNotification -> unit // will create email body from template and send it

let notificationProcessor (sendEmail: EmailSender) (mailbox: Actor<NotificationProcessorMsg>) =
    let rec loop () =
        actor {
            let! msg = mailbox.Receive()

            match msg with
            | SendEmailNotification notification -> sendEmail notification

            return! loop ()
        }

    loop ()

let notificationProcessorRef =
    spawnAnonymous system
    <| props (notificationProcessor (fun sen -> printfn $"Sending email to {sen.Subscriber.Email}"))

//===================================================================================================

type RegionDate = { Region: string; Date: DateTime }

type SubscriberList =
    { Region: string
      Date: DateTime
      Subscribers: Subscriber list }

type SubscriberFinderMsg = FindSubscribers of RegionDate


// find all subscribers, type Subscrber
// after returning results, for each one of them, push Notification message into the queue
let subscriberFinder targetRef (mailbox: Actor<SubscriberFinderMsg>) =
    let rec loop () =
        actor {
            let! msg = mailbox.Receive()

            match msg with
            | FindSubscribers findSubscribers ->
                targetRef
                <! { Region = findSubscribers.Region
                     Date = findSubscribers.Date
                     Subscribers =
                       [ { Name = "John Doe"
                           Email = "john@doe.com" }
                         { Name = "Jane Doe"
                           Email = "jane@doe.com" } ] }

            return! loop ()
        }

    loop ()

let spawnsubscriberFinder targetRef =
    spawnAnonymous system <| props (subscriberFinder targetRef)

//===================================================================================================

// subscriberFinder |> notificationProcessor

let pipeline =
    Source.actorRef OverflowStrategy.Backpressure 1000
    |> Source.mapMaterializedValue (spawnsubscriberFinder)
    |> Source.toMat
        (Sink.forEach (fun s ->
            s.Subscribers
            |> List.iter (fun subscriber ->
                notificationProcessorRef
                <! SendEmailNotification
                    { Subscriber = subscriber
                      EventName = "Christmas"
                      EmailMessage = "Merry Christmas!" })))
        Keep.left
    |> Graph.run mat

let regionDate: RegionDate =
    { Region = "Norway"
      Date = new DateTime(2023, 12, 25) }

pipeline <! (FindSubscribers regionDate)
