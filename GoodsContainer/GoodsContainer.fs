namespace ShoppingCart
open ShoppingCart.Commons
open System
open Sharpino

open MBrace.FsPickler.Json
open FsToolkit.ErrorHandling

module GoodsContainer =

    let pickler = FsPickler.CreateJsonSerializer(indent = false)
    type GoodsContainer(goodRefs: List<Guid>, cartRefs: List<Guid>, mySerializer: MySerializer<string>) =

module GoodsContainer =
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
            GoodsContainer (goodRefs, cartRef :: cartRefs) |> Ok

        static member Zero = GoodsContainer ([], [])
        static member StorageName = "_goodsContainer"
        static member Version = "_01"
        static member SnapshotsInterval = 15
        static member Deserialize json =
            globalSerializer.Deserialize<GoodsContainer> json 
        member this.Serialize =
            this |> globalSerializer.Serialize
