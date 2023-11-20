module rec HolidayTracker.Shared.Model

open System
open Fable.Validation
open FsToolkit.ErrorHandling
open Thoth.Json

let extraEncoders = Extra.empty |> Extra.withInt64 |> Extra.withDecimal

let inline forceValidate (e) =
    match e with
    | Ok x -> x
    | Error x ->
        let errors = x |> List.map (fun x -> x.ToString()) |> String.concat ", "
        invalidOp errors

let inline forceValidateWithString (e) =
    match e with
    | Ok x -> x
    | Error x -> invalidOp x

type Predicate =
    | Greater of string * IComparable
    | GreaterOrEqual of string * IComparable
    | Smaller of string * IComparable
    | SmallerOrEqual of string * IComparable
    | Equal of string * obj
    | NotEqual of string * obj
    | And of Predicate * Predicate
    | Or of Predicate * Predicate
    | Not of Predicate


type Version =
    | Version of int64

    member this.Value: int64 = let (Version v) = this in v
    member this.Zero = Version 0L

type ShortStringError =
    | EmptyString
    | TooLongString

type ShortString =
    private
    | ShortString of string

    member this.Value = let (ShortString s) = this in s

    static member TryCreate(s: string) =
        single (fun t ->
            t.TestOne s
            |> t.MinLen 1 ShortStringError.EmptyString
            |> t.MaxLen 255 ShortStringError.TooLongString
            |> t.Map ShortString
            |> t.End)

    static member Validate(s: ShortString) =
        s.Value |> ShortString.TryCreate |> forceValidate

    override this.ToString() = this.Value


type LongString =
    private
    | LongString of string

    member this.Value = let (LongString lng) = this in lng

    static member TryCreate(s: string) =
        single (fun t -> t.TestOne s |> t.MinLen 1 EmptyString |> t.Map LongString |> t.End)

    static member Validate(s: LongString) =
        s.Value |> LongString.TryCreate |> forceValidate

    override this.ToString() = this.Value


type CountryId =
    | CountryId of ShortString

    member this.Value = let (CountryId orderId) = this in orderId

    static member CreateNew() =
        "Country_" + Guid.NewGuid().ToString()
        |> ShortString.TryCreate
        |> forceValidate
        |> CountryId

type Country =
    { CountryId: CountryId
      Name: ShortString }
