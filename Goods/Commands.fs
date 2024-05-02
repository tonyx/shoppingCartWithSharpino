
namespace ShoppingCart 

open Sharpino.Core
open FsToolkit.ErrorHandling

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
                        |> Result.map (fun _ -> [PriceChanged price])
                    | ChangeDiscounts discounts ->
                        good.ChangeDiscounts discounts
                        |> Result.map (fun _ -> [DiscountsChanged discounts])
                    | AddQuantity quantity ->
                        good.AddQuantity quantity
                        |> Result.map (fun _ -> [QuantityAdded quantity])
                    | RemoveQuantity quantity ->
                        good.RemoveQuantity quantity
                        |> Result.map (fun _ -> [QuantityRemoved quantity])
                member this.Undoer = None
