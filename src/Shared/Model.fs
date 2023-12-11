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
    member _.Zero = Version 0L

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


module Authentication =
    open System.Text.RegularExpressions

    type EmailError =
        | EmptyEmail
        | InvalidEmailAddress

    type Email =
        private
        | Email of string

        member this.Value = let (Email email) = this in email

        static member TryCreate(email: string) =
            let regex =
                //regex not containing '_Saga_'
                Regex(@"^(?!.*(_dot_|_Saga_|~)).*$", RegexOptions.IgnoreCase)

            let email = email.Trim().Replace(" ", "")

            single (fun t ->
                t.TestOne email
                |> t.MinLen 1 EmptyEmail
                |> t.MaxLen 50 InvalidEmailAddress
                |> t.Match regex InvalidEmailAddress
                |> t.Map(fun x ->
                    let lowerCase = x.ToLowerInvariant()

                    let email =
                        if lowerCase.Contains("@gmail") && lowerCase.Contains(".") then
                            let left = lowerCase.Split("@").[0]
                            let right = lowerCase.Split("@").[1]
                            let removeDots = left.Replace(".", "")
                            removeDots + "@" + right
                        else
                            lowerCase

                    Email email)
                |> t.End)

        static member Validate(s: Email) =
            s.Value |> Email.TryCreate |> forceValidate

    type UserClientId = Email

    type User =
        { UserClientId: UserClientId
          Version: Version }

    type VerificationError =
        | EmptyVerificationCode
        | InvalidVerificationCode

    type VerificationCode =
        private
        | VerificationCode of string

        member this.Value = let (VerificationCode s) = this in s

        static member TryCreate(s: string) =
            single (fun t ->
                t.TestOne s
                |> t.MinLen 1 EmptyVerificationCode
                |> t.MaxLen 6 InvalidVerificationCode
                |> t.Map VerificationCode
                |> t.End)

    type LoginError = string
    type LogoutError = string

    type Subject = ShortString
    type Body = LongString

module Subscription = 
    type RegionType = Country

    type RegionId =
        | RegionId of ShortString
    
        member this.Value = let (RegionId rid) = this in rid
    
        static member CreateNew() =
            "Region_" + Guid.NewGuid().ToString()
            |> ShortString.TryCreate
            |> forceValidate
            |> RegionId
    
        static member Create(s: string) =
            s |> ShortString.TryCreate |> forceValidate |> RegionId
    
        static member Validate(s: LongString) =
            s.Value |> ShortString.TryCreate |> forceValidate
    
    type Region =
        { RegionId: RegionId
          RegionType: RegionType
          AlrernateNames: ShortString list
          Name: ShortString }
    