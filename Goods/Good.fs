namespace ShoppingCart
open System
open ShoppingCart.Commons
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
    let pickler = FsPickler.CreateJsonSerializer(indent = false)
    type Discount =
        { ItemNumber: int
          Price: decimal }

    type Discounts = List<Discount>

    type Good private (id: Guid, name: string, price: decimal, discounts: Discounts, quantity: int, mySerializer: MySerializer<string>) =
        let stateId = Guid.NewGuid()
        member this.StateId = stateId
        member this.Id = id
        member this.Name = name
        member this.Price = price
        member this.Discounts = discounts
        member this.Quantity = quantity

        [<JsonConstructor>]
        new (id: Guid, name: string, price: decimal, discounts: Discounts, mySerializer: MySerializer<string>) =
            Good (id, name, price, discounts, 0, mySerializer )

        member this.SetPrice (price: decimal) =
            Good (this.Id, this.Name, price, this.Discounts, quantity, mySerializer) |> Ok

        member this.ChangeDiscounts(discounts: Discounts) =
            Good (this.Id, this.Name, this.Price, discounts, quantity, mySerializer) |> Ok

        member this.AddQuantity(quantity: int) =
            Good (this.Id, this.Name, this.Price, this.Discounts, this.Quantity + quantity, mySerializer) |> Ok

        member this.RemoveQuantity(quantity: int) =
            result {
                do! 
                    this.Quantity - quantity >= 0
                    |> Result.ofBool "Quantity not available"
                return Good (this.Id, this.Name, this.Price, this.Discounts, this.Quantity - quantity, mySerializer)
            }

        static member StorageName = "_good"
        static member Version = "_01"
        static member SnapshotsInterval = 15 
        static member Deserialize (serializer, json) = // (serializer: ISerializer, json: 'F) =
            globalSerializer.Deserialize json
        member this.Serialize(serializer: ISerializer) =
            globalSerializer.Serialize this
            // mySerializer.Serialize this

        interface Aggregate<string> with
            member this.Id = this.Id
            member this.Serialize (serializer: ISerializer) =
                this.Serialize serializer
            member this.Lock = this
            member this.StateId = this.StateId

        interface Entity with
            member this.Id = this.Id
