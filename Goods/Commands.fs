
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
open ShoppingCart.GoodEvents

module GoodCommands =
    type GoodCommands =
        | ChangePrice of decimal
        | ChangeDiscounts of List<Good.Discount>
        | AddQuantity of int
        | RemoveQuantity of int
            interface Command<Good, GoodEvents> with
                member this.Execute (good: Good) =
                    match this with
                    | ChangePrice price -> 
                        good.SetPrice price
                        |> Result.map (fun x -> [PriceChanged price])
                    | ChangeDiscounts discounts ->
                        good.ChangeDiscounts discounts
                        |> Result.map (fun x -> [DiscountsChanged discounts])
                    | AddQuantity quantity ->
                        good.AddQuantity quantity
                        |> Result.map (fun x -> [QuantityAdded quantity])
                    | RemoveQuantity quantity ->
                        good.RemoveQuantity quantity
                        |> Result.map (fun x -> [QuantityRemoved quantity])
                member this.Undoer = None
