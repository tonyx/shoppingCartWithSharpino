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

open ShoppingCart.Good

module GoodEvents =
    type GoodEvents =   
    | PriceChanged of decimal
    | DiscountsChanged of List<Good.Discount>
        interface Event<Good> with
            member this.Process (good: Good) =
                match this with
                | PriceChanged price -> good.SetPrice price
                | DiscountsChanged discounts -> good.ChangeDiscounts discounts

        static member Deserialize (serializer: ISerializer, json: string) =
            serializer.Deserialize<GoodEvents>(json)
        member this.Serialize(serializer: ISerializer) =
            this |> serializer.Serialize


