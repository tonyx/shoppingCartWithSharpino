
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
    type CartEvents =
    | GoodAdded of Guid * int
        interface Event<Cart> with
            member this.Process (cart: Cart) =
                match this with
                | GoodAdded (goodRef, quantity) -> cart.AddGood (goodRef, quantity)

        static member Deserialize (serializer: ISerializer, json: string) =
            serializer.Deserialize<CartEvents>(json)
        member this.Serialize(serializer: ISerializer) =
            this |> serializer.Serialize