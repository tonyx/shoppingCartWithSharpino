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

module GoodsContainer =

    let pickler = FsPickler.CreateJsonSerializer(indent = false)
    type GoodsContainer(goodRefs: List<Guid>, cartRefs: List<Guid>) =

        let stateId = Guid.NewGuid()
        member this.StateId = stateId
        member this.GoodRefs = goodRefs
        member this.CartRefs = cartRefs

        member this.AddGood(goodRef: Guid) =
            result {
                do! 
                    this.GoodRefs 
                    |> List.contains goodRef
                    |> not
                    |> Result.ofBool "Good already in items list"
                return GoodsContainer(goodRef :: goodRefs, cartRefs)
            }

        member this.RemoveGood(goodRef: Guid) =
            result {
                do! 
                    this.GoodRefs 
                    |> List.contains goodRef
                    |> Result.ofBool "Good not in items list"
                return GoodsContainer(goodRefs |> List.filter (fun x -> x <> goodRef), cartRefs)
            }

        member this.AddCart (cartRef: Guid) =
            GoodsContainer(goodRefs, cartRef :: cartRefs) |> Ok

        static member Zero = GoodsContainer([], [])
        static member StorageName = "_goodsContainer"
        static member Version = "_01"
        static member SnapshotsInterval = 15
        static member Lock =
            new Object()
        static member Deserialize (serializer: ISerializer, json: string) =
            try 
                pickler.UnPickleOfString<GoodsContainer> json |> Ok
            with
            | ex -> Error ex.Message
        member this.Serialize(serializer: ISerializer) =
            pickler.PickleToString this




