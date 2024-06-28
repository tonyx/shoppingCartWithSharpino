namespace ShoppingCart

open ShoppingCart.Good
open ShoppingCart.Commons
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
open FSharpPlus
open FsToolkit.ErrorHandling

module Supermarket =
    open Sharpino.CommandHandler
    let doNothingBroker: IEventBroker<string> =
        {  notify = None
           notifyAggregate = None }

    type Supermarket (eventStore: IEventStore<string>, eventBroker: IEventBroker<string>) =
        let goodsContainerViewer = getStorageFreshStateViewer<GoodsContainer, GoodsContainerEvents, string> eventStore
        let goodsViewer = getAggregateStorageFreshStateViewer<Good, GoodEvents, string> eventStore
        let cartViewer = getAggregateStorageFreshStateViewer<Cart, CartEvents, string> eventStore

let doNothingBroker: IEventBroker<'F> =
        {  notify = None
           notifyAggregate = None }

    // type Supermarket (eventStore: IEventStore<'F>, eventBroker: IEventBroker<_>) =
    type Supermarket (eventStore: IEventStore<'F>, eventBroker: IEventBroker<_>, goodsViewer: AggregateViewer<Good>, cartViewer: AggregateViewer<Cart>, goodsContainerViewer: StateViewer<GoodsContainer>) =
        let goodsContainerViewer = getStorageFreshStateViewer<GoodsContainer, GoodsContainerEvents, 'F> eventStore
        // let goodsViewer = getAggregateStorageFreshStateViewer<Good, GoodEvents, 'F> eventStore
        // let cartViewer = getAggregateStorageFreshStateViewer<Cart, CartEvents, 'F> eventStore

        member this.GoodRefs = 
            result {
                let! (_, state) = goodsContainerViewer ()
                return state.GoodRefs
            }
        member this.CartRefs =
            result {
                let! (_, state) = goodsContainerViewer ()
                return state.CartRefs
            }

        member this.GetGoodsQuantity (goodId: Guid) = 
            result {
                let! esists = this.GetGood goodId
                let! (_, state) = goodsViewer goodId
                return state.Quantity
            }

        member this.AddQuantity (goodId: Guid, quantity: int) = 
            result {
                let! (_, state) = goodsViewer goodRef
                let command = GoodCommands.AddQuantity  quantity
                return! 
                    command 
                    |> runAggregateCommand<Good, GoodEvents, string> goodRef eventStore eventBroker
            }

        member this.GetGood (id: Guid) = 
            result {
                let! goods = this.GoodRefs
                let! goodExist = 
                    goods
                    |> List.tryFind (fun g -> g = id)
                    |> Result.ofOption "Good not found"
                let! (_, state) = goodsViewer goodRef
                return state
            }
        member this.Goods =
            result {
                let! (_, state) = goodsContainerViewer ()

                // warning: if there is a ref to an unexisting good you are in trouble. fix it
                let! goods =
                    state.GoodRefs
                    |> List.map this.GetGood
                    |> Result.sequence
                return goods |> Array.toList
            }

        member this.AddGood (good: Good) =  
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
                    |> runInitAndCommand<GoodsContainer, GoodsContainerEvents, Good, 'F> eventStore eventBroker good
                return! Ok ()
            }

        member this.RemoveGood (id: Guid) = 
            result {
                let! good = this.GetGood goodRef
                let! (_, state) = goodsContainerViewer ()
                let command = GoodsContainerCommands.RemoveGood goodRef
                return! 
                    command
                    |> runCommand<GoodsContainer, GoodsContainerEvents, string> eventStore eventBroker 
            }

        member this.AddCart (cart: Cart) = 
            result {
                return! 
                    cart.Id
                    |> AddCart
                    |> runInitAndCommand<GoodsContainer, GoodsContainerEvents, Cart, string> eventStore eventBroker cart
            }

        member this.GetCart (cartRef: Guid) = 
            result {
                let! cartRefs = this.CartRefs
                let! exists =
                    cartRefs
                    |> List.tryFind (fun c -> c = cartRef)
                    |> Result.ofOption "Cart not found"
                let! (_, state) = cartViewer cartRef
                return state
            }

        member this.AddGoodToCart (cartId: Guid, goodId: Guid, quantity: int) =
            result {
                let removeQuantity: Command<Good, GoodEvents> = GoodCommands.RemoveQuantity quantity
                let addGood: Command<Cart, CartEvents> = CartCommands.AddGood (goodId, quantity) 
                return! 
                    runTwoNAggregateCommands 
                        [goodId]
                        [cartId] 
                        eventStore 
                        eventBroker 
                        [removeQuantity] 
                        [addGood] 
            }

        member this.AddGoodsToCart (cartId: Guid, goods: (Guid * int) list) =
            result {
                let commands = 
                    goods
                    |> List.map (fun (goodId, quantity) -> 
                        let removeQuantity: Command<Good, GoodEvents> = GoodCommands.RemoveQuantity quantity
                        let addGood: Command<Cart, CartEvents> = CartCommands.AddGood (goodId, quantity) 
                        (removeQuantity, addGood)
                    )
                let goodIds = goods |>> fst
                let commandRemove = commands |>> fst
                let commandAdd = commands |>> snd
                let size = goods.Length

                let cartids =
                    [1..size] |>> (fun _ -> cartId)
                
                return!
                    runTwoNAggregateCommands 
                        goodIds
                        cartids
                        eventStore 
                        eventBroker 
                        commandRemove
                        commandAdd
            }


  