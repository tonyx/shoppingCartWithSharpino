
namespace ShoppingCart 

open System
open Sharpino
open Sharpino.Core
open Sharpino.Lib.Core.Commons
open Sharpino.Utils
open Sharpino.Result
open Sharpino.Definitions
open FSharpPlus
open MBrace.FsPickler.Json
open FsToolkit.ErrorHandling
open MBrace.FsPickler.Combinators

open ShoppingCart.Cart

module CartEvents =
    let pickler = FsPickler.CreateJsonSerializer(indent = false)
    type CartEvents =
    | GoodAdded of Guid * int
        interface Event<Cart> with
            member this.Process (cart: Cart) =
                match this with
                | GoodAdded (goodRef, quantity) -> cart.AddGood (goodRef, quantity)

        static member Deserialize (serializer: ISerializer, json: string) =
            try
                pickler.UnPickleOfString<CartEvents> json |> Ok
            with
            | ex -> Error ex.Message

        member this.Serialize(serializer: ISerializer) =
            pickler.PickleToString this