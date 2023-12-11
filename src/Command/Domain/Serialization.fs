module Command.Serialization

open Command
open Akkling
open Akka.Actor
open Akka.Serialization
open System.Text
open NodaTime
open Thoth.Json.Net
open System.Runtime.Serialization
open Serilog
open System
open HolidayTracker.Command.Domain

module DefaultEncode =
    let instant (instant: Instant) =
        Encode.datetime (instant.ToDateTimeUtc())

module DefeaultDecode =
    let instant: Decoder<Instant> =
        Decode.datetimeUtc |> Decode.map (Instant.FromDateTimeUtc)

let extraThoth =
    Extra.empty
    |> Extra.withInt64
    |> Extra.withDecimal
    |> Extra.withCustom (DefaultEncode.instant) DefeaultDecode.instant

let userIdentityMessageEncode =
    Encode.Auto.generateEncoder<Common.Event<UserIdentity.Event>> (extra = extraThoth)

let userIdentityMessageDecode =
    Decode.Auto.generateDecoder<Common.Event<UserIdentity.Event>> (extra = extraThoth)

/// State encoding
let userIdentityStateEncode =
    Encode.Auto.generateEncoder<UserIdentity.State> (extra = extraThoth)

let userIdentityStateDecode =
    Decode.Auto.generateDecoder<UserIdentity.State> (extra = extraThoth)


type ThothSerializer(system: ExtendedActorSystem) =
    inherit SerializerWithStringManifest(system)

    override _.Identifier = 1712

    override _.ToBinary(o) =

        match o with
        | :? Common.Event<UserIdentity.Event> as mesg -> mesg |> userIdentityMessageEncode
        | :? UserIdentity.State as mesg -> mesg |> userIdentityStateEncode

        | e ->
            Log.Fatal("shouldn't happen {e}", e)
            Environment.FailFast("shouldn't happen")
            failwith "shouldn't happen"
        |> Encode.toString 4
        |> Encoding.UTF8.GetBytes

    override _.Manifest(o: obj) : string =
        match o with
        | :? Common.Event<UserIdentity.Event> -> "UserIdentityMessage"
        | :? UserIdentity.State -> "UserIdentityState"
        | _ -> o.GetType().FullName

    override _.FromBinary(bytes: byte[], manifest: string) : obj =
        let decode decoder =
            Encoding.UTF8.GetString(bytes)
            |> Decode.fromString decoder
            |> function
                | Ok res -> res
                | Error er -> raise (new SerializationException(er))

        match manifest with
        | "UserIdentityState" -> upcast decode userIdentityStateDecode
        | "UserIdentityMessage" -> upcast decode userIdentityMessageDecode

        | _ ->
            Log.Fatal("manifest {manifest} not found", manifest)
            Environment.FailFast("shouldn't happen")
            raise (new SerializationException())
