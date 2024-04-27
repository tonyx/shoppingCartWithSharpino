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

    type GoodsContainer(goodRefs: List<Guid>, quantities: Map<Guid, int>, cartRefs: List<Guid>) =

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
                return GoodsContainer(goodRef :: goodRefs, quantities, cartRefs)
            }

        member this.RemoveGood(goodRef: Guid) =
            result {
                do! 
                    this.GoodRefs 
                    |> List.contains goodRef
                    |> Result.ofBool "Good not in items list"
                return GoodsContainer(goodRefs |> List.filter (fun x -> x <> goodRef), quantities, cartRefs)
            }

        member this.SetQuantity(goodRef: Guid, quantity: int) =
            result {
                do! 
                    this.GoodRefs 
                    |> List.contains goodRef
                    |> Result.ofBool "Good not in items list"
                do!
                    quantities.Keys.Contains goodRef
                    |> not
                    |> Result.ofBool "Good not in quantities list"
                return GoodsContainer(goodRefs, quantities |> Map.add goodRef quantity, cartRefs)
            }

        member this.AddCart (cartRef: Guid) =
            GoodsContainer(goodRefs, quantities, cartRef :: cartRefs) |> Ok

        member this.GetQuantity (goodRef: Guid) =
            quantities.TryFind goodRef
            |> Result.ofOption "Good not in quantities list"

        static member Zero = GoodsContainer([], [] |> Map.ofList, [])
        static member StorageName = "_goodsContainer"
        static member Version = "_01"
        static member SnapshotsInterval = 15
        static member Lock =
            new Object()
        static member Deserialize (serializer: ISerializer, json: string) =
            serializer.Deserialize<GoodsContainer>(json)
        member this.Serialize(serializer: ISerializer) =
            serializer.Serialize this




