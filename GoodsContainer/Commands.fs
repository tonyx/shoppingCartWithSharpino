namespace ShoppingCart

open System
open Sharpino
open Sharpino.Core
open Sharpino.Utils
open Sharpino.Result
open Sharpino.Definitions
open FSharpPlus
open MBrace.FsPickler.Json
open FsToolkit.ErrorHandling
open ShoppingCart.GoodsContainer
open ShoppingCart.GoodsContainerEvents

module GoodsContainerCommands =
    type GoodsContainerCommands =
        | AddGood of Guid
        | RemoveGood of Guid
        | SetQuantity of Guid * int
        | AddCart of Guid
        | AddQuantity of Guid * int
        | RemoveQuantity of Guid * int
            interface Command<GoodsContainer, GoodsContainerEvents> with
                member this.Execute (goodsContainer: GoodsContainer) =
                    match this with
                    | AddGood goodRef -> 
                        goodsContainer.AddGood goodRef
                        |> Result.map (fun _ -> [GoodAdded goodRef])
                    | RemoveGood goodRef ->
                        goodsContainer.RemoveGood goodRef
                        |> Result.map (fun _ -> [GoodRemoved goodRef])
                    | SetQuantity (goodRef, quantity) -> 
                        goodsContainer.SetQuantity (goodRef, quantity)
                        |> Result.map (fun _ -> [QuantityChanged (goodRef, quantity)])
                    | AddCart cartRef ->
                        goodsContainer.AddCart cartRef
                        |> Result.map (fun _ -> [CartAdded cartRef])
                    | AddQuantity (goodRef, quantity) ->
                        goodsContainer.AddQuantity (goodRef, quantity)
                        |> Result.map (fun _ -> [QuantityAdded (goodRef, quantity)])
                    | RemoveQuantity (goodRef, quantity) ->
                        goodsContainer.RemoveQuantity (goodRef, quantity)
                        |> Result.map (fun _ -> [QuantityRemoved (goodRef, quantity)])
                member this.Undoer = None


