namespace ShoppingCart 
open ShoppingCart.Commons
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
    let pickler = FsPickler.CreateJsonSerializer(indent = false)
    type GoodEvents =   
    | PriceChanged of decimal
    | DiscountsChanged of List<Good.Discount>
    | QuantityAdded of int
    | QuantityRemoved of int
     
        interface Event<Good> with
            member this.Process (good: Good) =
                match this with
                | PriceChanged price -> good.SetPrice price
                | DiscountsChanged discounts -> good.ChangeDiscounts discounts
                | QuantityAdded quantity -> good.AddQuantity quantity
                | QuantityRemoved quantity -> good.RemoveQuantity quantity

        static member Deserialize (serializer: ISerializer, json: 'F) =
            globalSerializer.Deserialize<GoodEvents> json // |> Ok

        member this.Serialize(serializer: ISerializer) =
            globalSerializer.Serialize this


