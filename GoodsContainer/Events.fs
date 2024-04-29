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
    let pickler = FsPickler.CreateJsonSerializer(indent = false)
    type GoodsContainerEvents =
        | GoodAdded of Guid
        | GoodRemoved of Guid
        | CartAdded of Guid
            interface Event<GoodsContainer> with
                member this.Process (goodsContainer: GoodsContainer) =
                    match this with
                    | GoodAdded goodRef -> goodsContainer.AddGood goodRef
                    | GoodRemoved goodRef -> goodsContainer.RemoveGood goodRef
                    | CartAdded cartRef -> goodsContainer.AddCart cartRef

        static member Deserialize (serializer: ISerializer, json: string) =
            try
                pickler.UnPickleOfString<GoodsContainerEvents> json |> Ok
            with
            | ex -> Error ex.Message
        member this.Serialize(serializer: ISerializer) =
            pickler.PickleToString this
