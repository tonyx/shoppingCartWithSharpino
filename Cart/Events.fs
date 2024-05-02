
namespace ShoppingCart 

open ShoppingCart.Commons  
open ShoppingCart.Cart
open System
open Sharpino.Core

module CartEvents =
    type CartEvents =
    | GoodAdded of Guid * int
        interface Event<Cart> with
            member this.Process (cart: Cart) =
                match this with
                | GoodAdded (goodRef, quantity) -> cart.AddGood (goodRef, quantity)

        static member Deserialize  json =
            globalSerializer.Deserialize<CartEvents> json // |> Ok

        member this.Serialize =
            globalSerializer.Serialize this