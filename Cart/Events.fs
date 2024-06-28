
namespace ShoppingCart 

open ShoppingCart.Commons  
open ShoppingCart.Cart
open System
open Sharpino.Core
open ShoppingCart.Commons
open MBrace.FsPickler.Json

open ShoppingCart.Cart

module CartEvents =
    type CartEvents =
    | GoodAdded of Guid * int
        interface Event<Cart> with
            member this.Process (cart: Cart) =
                match this with
                | GoodAdded (goodRef, quantity) -> cart.AddGood (goodRef, quantity)

        static member Deserialize json =
            globalSerializer.Deserialize<CartEvents> json

        member this.Serialize =
            globalSerializer.Serialize this
