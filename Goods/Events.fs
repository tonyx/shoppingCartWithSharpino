namespace ShoppingCart 
open ShoppingCart.Commons
open ShoppingCart.Good
open Sharpino.Core

module GoodEvents =
    type GoodEvents =   
    | PriceChanged of decimal
    | DiscountsChanged of List<Good.Discount>
    | QuantityAdded of int
    | QuantityRemoved of int
     
        interface Event<Good> with
            member this.Process (good: Good) =
                match this with
                | PriceChanged price -> good.SetPrice price
                | DiscountsChanged discounts -> good.ChangeDiscounts discounts
                | QuantityAdded quantity -> good.AddQuantity quantity
                | QuantityRemoved quantity -> good.RemoveQuantity quantity

        static member Deserialize x =
            x |> globalSerializer.Deserialize<GoodEvents> 

        member this.Serialize =
            this |> globalSerializer.Serialize
