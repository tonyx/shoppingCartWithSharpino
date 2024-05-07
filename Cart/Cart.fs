
namespace ShoppingCart
open ShoppingCart.Commons
open System
open Sharpino
open Sharpino.Core
open FsToolkit.ErrorHandling

module Cart =
    type Cart (id: Guid, goods: Map<Guid, int>) =
        let stateId = Guid.NewGuid ()

        member this.StateId = stateId
        member this.Id = id
        member this.Goods = goods

        member this.AddGood (goodRef: Guid, quantity: int) =
            Cart (this.Id, this.Goods.Add(goodRef, quantity)) |> Ok
        member this.GetGoodAndQuantity (goodRef: Guid) =
            result {
                let! goodExists =
                    this.Goods.ContainsKey goodRef
                    |> Result.ofBool "Good not in cart"
                let quantity =
                    this.Goods.[goodRef]
                return quantity
            }

        static member StorageName = "_cart" 
        static member Version = "_01"
        static member SnapshotsInterval = 15
        static member Deserialize x =
            x |> globalSerializer.Deserialize<Cart> 
        member this.Serialize =
            globalSerializer.Serialize this

        interface Aggregate<string> with
            member this.Id = this.Id
            member this.Serialize =
                this.Serialize
            member this.Lock = this
            member this.StateId = this.StateId
        



