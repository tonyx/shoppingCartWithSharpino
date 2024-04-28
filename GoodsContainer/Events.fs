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

module GoodsContainerEvents =
    type GoodsContainerEvents =
        | GoodAdded of Guid
        | GoodRemoved of Guid
        | CartAdded of Guid
        | QuantityChanged of Guid * int
        | QuantityAdded of Guid * int
        | QuantityRemoved of Guid * int
            interface Event<GoodsContainer> with
                member this.Process (goodsContainer: GoodsContainer) =
                    match this with
                    | GoodAdded goodRef -> goodsContainer.AddGood goodRef
                    | GoodRemoved goodRef -> goodsContainer.RemoveGood goodRef
                    | QuantityChanged (goodRef, quantity) -> 
                        goodsContainer.SetQuantity (goodRef, quantity)
                    | CartAdded cartRef -> goodsContainer.AddCart cartRef
                    | QuantityAdded (goodRef, quantity) -> 
                        goodsContainer.AddQuantity (goodRef, quantity)
                    | QuantityRemoved (goodRef, quantity) -> 
                        goodsContainer.RemoveQuantity (goodRef, quantity)

        static member Deserialize (serializer: ISerializer, json: string) =
            serializer.Deserialize<GoodsContainerEvents>(json)
        member this.Serialize(serializer: ISerializer) =
            this |> serializer.Serialize
