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
        | AddCart of Guid
            interface Command<GoodsContainer, GoodsContainerEvents> with
                member this.Execute (goodsContainer: GoodsContainer) =
                    match this with
                    | AddGood goodRef -> 
                        goodsContainer.AddGood goodRef
                        |> Result.map (fun _ -> [GoodAdded goodRef])
                    | RemoveGood goodRef ->
                        goodsContainer.RemoveGood goodRef
                        |> Result.map (fun _ -> [GoodRemoved goodRef])
                    | AddCart cartRef ->
                        goodsContainer.AddCart cartRef
                        |> Result.map (fun _ -> [CartAdded cartRef])
                member this.Undoer = None


