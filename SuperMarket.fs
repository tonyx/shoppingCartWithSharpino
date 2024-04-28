namespace ShoppingCart

open ShoppingCart.Good
open ShoppingCart.GoodEvents
open ShoppingCart.GoodCommands
open ShoppingCart.GoodsContainer
open ShoppingCart.GoodsContainerEvents
open ShoppingCart.GoodsContainerCommands
open ShoppingCart.Cart
open ShoppingCart.CartEvents

open System
open Sharpino
open Sharpino.Storage
open Sharpino.Core
open Sharpino.Lib.Core.Commons
open Sharpino.Utils
open Sharpino.Core
open Sharpino.Utils
open Sharpino.Result
open Sharpino.PgStorage
open FsToolkit.ErrorHandling

module Supermarket =
    open Sharpino.CommandHandler
    let doNothingBroker: IEventBroker =
        {  notify = None
           notifyAggregate = None }

    type Supermarket(eventStore: IEventStore, eventBroker: IEventBroker) =
        let goodsContainerViewer = getStorageFreshStateViewer<GoodsContainer,GoodsContainerEvents> eventStore
        let goodsViewer = getAggregateStorageFreshStateViewer<Good,GoodEvents> eventStore
        let cartViewer = getAggregateStorageFreshStateViewer<Cart,CartEvents> eventStore

        member this.GoodRefs = 
            result {
                let! (_, state, _ , _) = goodsContainerViewer ()
                return state.GoodRefs
            }

        member private this.SetGoodsQuantity (goodRef: Guid, quantity: int) = 
            result {
                return! 
                    (goodRef, quantity)
                    |> SetQuantity
                    |> runCommand<GoodsContainer, GoodsContainerEvents> eventStore eventBroker goodsContainerViewer
            }
        member this.GetGoodsQuantity (goodRef: Guid) = 
            result {
                let! (_, state, _, _) = goodsContainerViewer ()
                return! state.GetQuantity goodRef
            }

        member this.AddQuantity (goodRef: Guid, quantity: int) = 
            result {
                let command = AddQuantity (goodRef, quantity)
                return! 
                    command
                    |> runCommand<GoodsContainer, GoodsContainerEvents> eventStore eventBroker goodsContainerViewer
            }

        member this.GetGood (goodRef: Guid) = 
            result {
                let! (_, state, _, _) = goodsViewer goodRef
                return state
            }
        member this.Goods =
            result {
                let! (_, state, _, _) = goodsContainerViewer ()
                let! goods =
                    state.GoodRefs
                    |> List.map this.GetGood
                    |> Result.sequence
                return goods |> Array.toList
            }

        member this.AddGood (good: Good) =  
            // we may want to taste the transactionality here as there are two operations
            // this inspire to add a features like runInitAndTwoCommands
            result {
                let! goodAdded =
                    good.Id 
                    |> AddGood 
                    |> runInitAndCommand<GoodsContainer, GoodsContainerEvents, Good> eventStore eventBroker goodsContainerViewer good
                let! quantitySet =
                    (good.Id, 0)
                    |> SetQuantity
                    |> runCommand<GoodsContainer, GoodsContainerEvents> eventStore eventBroker goodsContainerViewer
                return! Ok ()
            }

        member this.AddCart (cart: Cart) = 
            result {
                return! 
                    cart.Id
                    |> AddCart
                    |> runInitAndCommand<GoodsContainer, GoodsContainerEvents, Cart> eventStore eventBroker goodsContainerViewer cart
            }

        member this.GetCart (cartRef: Guid) = 
            result {
                let! (_, state, _, _) = cartViewer cartRef
                return state
            }

        member this.AddGoodToCart (cartRef: Guid, goodRef: Guid, quantity: int) = 
            result {
                let! addedToCart =
                    (CartCommands.AddGood (goodRef, quantity))
                    |> runAggregateCommand<Cart, CartEvents> cartRef eventStore eventBroker cartViewer

                let! removed =
                    RemoveQuantity (goodRef, quantity)
                    |> runCommand<GoodsContainer, GoodsContainerEvents> eventStore eventBroker goodsContainerViewer
                return removed
            }


