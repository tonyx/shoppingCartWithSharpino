
namespace ShoppingCart
open ShoppingCart.Commons
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

module Cart =
    let pickler = FsPickler.CreateJsonSerializer (indent = false)
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
        static member Deserialize json =
            globalSerializer.Deserialize<Cart> json
        member this.Serialize =
            globalSerializer.Serialize this

        interface Aggregate<byte[]> with
            member this.Id = this.Id
            member this.Serialize =
                this.Serialize
            member this.Lock = this
            member this.StateId = this.StateId
        
        interface Entity with
            member this.Id = this.Id



