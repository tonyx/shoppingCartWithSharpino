
namespace ShoppingCart 

open System
open Sharpino.Core
open FsToolkit.ErrorHandling
open ShoppingCart.Cart
open ShoppingCart.CartEvents

module CartCommands =
    type CartCommands =
    | AddGood of Guid * int

        interface Command<Cart, CartEvents> with
            member this.Execute (cart: Cart) =
                match this with
                | AddGood (goodRef, quantity) -> 
                    cart.AddGood (goodRef, quantity)
                    |> Result.map (fun _ -> [GoodAdded (goodRef, quantity)])
            member this.Undoer = None


