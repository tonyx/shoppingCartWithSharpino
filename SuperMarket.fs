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

        member this.GetGoodsQuantity (goodRef: Guid) = 
            result {
                let! (_, state, _, _) = goodsViewer goodRef
                return state.Quantity
            }

        member this.AddQuantity (goodRef: Guid, quantity: int) = 
            result {
                let! (_, state, _, _) = goodsViewer goodRef
                let command = GoodCommands.AddQuantity  quantity
                return! 
                    command 
                    |> runAggregateCommand<Good, GoodEvents> goodRef eventStore eventBroker goodsViewer
            }

        member this.GetGood (goodRef: Guid) = 
            result {
                let! (_, state, _, _) = goodsViewer goodRef
                return state
            }
        member this.Goods =
            result {
                let! (_, state, _, _) = goodsContainerViewer ()

                // warning: if there is a ref to an unexisting good you are in trouble. fix it
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
                let existingGoods = 
                    this.Goods
                    |> Result.defaultValue []
                do! 
                    existingGoods
                    |> List.exists (fun g -> g.Name = good.Name)
                    |> not
                    |> Result.ofBool "Good already in items list"

                let! goodAdded =
                    good.Id 
                    |> AddGood 
                    |> runInitAndCommand<GoodsContainer, GoodsContainerEvents, Good> eventStore eventBroker goodsContainerViewer good
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
                // under comment the code as it was before ingroducing runTwoNAggregateCommands

                let! good = this.GetGood goodRef
                let! cart = this.GetCart cartRef
                let removeAQuantity: Command<Good, GoodEvents> = GoodCommands.RemoveQuantity quantity
                let! removedFromGood = 
                    [removeAQuantity]
                    |> runNAggregateCommands<Good, GoodEvents> [goodRef] eventStore eventBroker goodsViewer
                let commandAddAQuantity: Command<Cart, CartEvents> = CartCommands.AddGood (goodRef, quantity) 
                let! addedToCart = 
                    [commandAddAQuantity]
                    |> runNAggregateCommands<Cart, CartEvents> [cartRef] eventStore eventBroker cartViewer


                // let commandRemoveQuantity: Command<Good, GoodEvents> = GoodCommands.RemoveQuantity quantity
                // let commandAddGood: Command<Cart, CartEvents> = CartCommands.AddGood (goodRef, quantity) 

                // let! moveFromGoodToCart =
                //     runTwoNAggregateCommands 
                //         [goodRef]
                //         [cartRef] 
                //         eventStore 
                //         eventBroker 
                //         goodsViewer 
                //         cartViewer
                //         [commandRemoveQuantity] 
                //         [commandAddGood] 
                return Ok

            }


