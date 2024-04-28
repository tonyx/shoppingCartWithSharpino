namespace ShoppingCart
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
open Newtonsoft.Json

module Good =
    type Discount =
        { ItemNumber: int
          Price: decimal }

    type Discounts = List<Discount>

    type Good private (id: Guid, name: string, price: decimal, discounts: Discounts, quantity: int) =
        let stateId = Guid.NewGuid()
        member this.StateId = stateId
        member this.Id = id
        member this.Name = name
        member this.Price = price
        member this.Discounts = discounts
        member this.Quantity = quantity

        [<JsonConstructor>]
        new (id: Guid, name: string, price: decimal, discounts: Discounts) =
            Good (id, name, price, discounts, 0)

        member this.SetPrice (price: decimal) =
            Good (this.Id, this.Name, price, this.Discounts, quantity) |> Ok

        member this.ChangeDiscounts(discounts: Discounts) =
            Good (this.Id, this.Name, this.Price, discounts, quantity) |> Ok

        member this.AddQuantity(quantity: int) =
            Good (this.Id, this.Name, this.Price, this.Discounts, this.Quantity + quantity) |> Ok

        member this.RemoveQuantity(quantity: int) =
            result {
                do! 
                    this.Quantity - quantity >= 0
                    |> Result.ofBool "Quantity not available"
                return Good (this.Id, this.Name, this.Price, this.Discounts, this.Quantity - quantity)
            }

        static member StorageName = "_good"
        static member Version = "_01"
        static member SnapshotsInterval = 15 
        static member Deserialize (serializer: ISerializer, json: string) =
            serializer.Deserialize<Good>(json)
        member this.Serialize(serializer: ISerializer) =
            serializer.Serialize this
        interface Aggregate with
            member this.Id = this.Id
            member this.Serialize (serializer: ISerializer) =
                this.Serialize serializer
            member this.Lock = this
            member this.StateId = this.StateId

        interface Entity with
            member this.Id = this.Id
