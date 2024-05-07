namespace ShoppingCart
open System
open ShoppingCart.Commons
open Sharpino
open Sharpino.Core
open FsToolkit.ErrorHandling

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

        new (id: Guid, name: string, price: decimal, discounts: Discounts ) =
            Good (id, name, price, discounts, 0 )

        member this.SetPrice (price: decimal) =
            Good (this.Id, this.Name, price, this.Discounts, quantity) |> Ok

        member this.ChangeDiscounts(discounts: Discounts) =
            Good (this.Id, this.Name, this.Price, discounts, quantity ) |> Ok

        member this.AddQuantity(quantity: int) =
            Good (this.Id, this.Name, this.Price, this.Discounts, this.Quantity + quantity ) |> Ok

        member this.RemoveQuantity(quantity: int) =
            result {
                do! 
                    this.Quantity - quantity >= 0
                    |> Result.ofBool "Quantity not available"
                return Good (this.Id, this.Name, this.Price, this.Discounts, this.Quantity - quantity )
            }

        static member StorageName = "_good"
        static member Version = "_01"
        static member SnapshotsInterval = 15 
        static member Deserialize x =
            x |> globalSerializer.Deserialize
        member this.Serialize =
            globalSerializer.Serialize this

        interface Aggregate<string> with
            member this.Id = this.Id
            member this.Serialize =
                this.Serialize 
            member this.Lock = this
            member this.StateId = this.StateId

