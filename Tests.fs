module Tests

open ShoppingCart.Good
open ShoppingCart.GoodEvents
open ShoppingCart.GoodCommands
open ShoppingCart.GoodsContainer
open ShoppingCart.GoodsContainerEvents
open ShoppingCart.GoodsContainerCommands
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
open Expecto
open Sharpino
open Sharpino.MemoryStorage
open ShoppingCart
open ShoppingCart.Supermarket
open ShoppingCart
open ShoppingCart.Cart
open Sharpino

[<Tests>]
let tests =
    let setUp (eventStore: IEventStore) =
        eventStore.Reset Good.Version Good.StorageName
        eventStore.Reset GoodsContainer.Version GoodsContainer.StorageName
        eventStore.ResetAggregateStream Good.Version Good.StorageName

    let connection = 
            "Server=127.0.0.1;" +
            "Database=es_shopping_cart;" +
            "User Id=safe;"+
            "Password=safe;"

    // let eventStore = MemoryStorage()
    let eventStore = PgEventStore(connection)

    testList "samples" [

        testCase "there are no good in a Supermarket" <| fun _ ->
            setUp eventStore
            let superMarket =  Supermarket(eventStore, doNothingBroker)
            let goods = superMarket.Goods

            Expect.isOk goods "should be ok"
            Expect.equal goods.OkValue [] "There are no goods in the supermarket."
        
        testCase "add a good to the supermarket and retrieve it" <| fun _ ->
            setUp eventStore
            let superMarket =  Supermarket(eventStore, doNothingBroker)
            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let added = superMarket.AddGood good
            Expect.isOk added "should be ok"
            let retrieved = superMarket.GetGood good.Id
            Expect.isOk retrieved "should be ok"
            let retrieved' = retrieved.OkValue
            Expect.equal  retrieved'.Id good.Id "should be the same good"

        testCase "add a good and put it in the supermarket" <| fun _ ->
            setUp eventStore
            let superMarket =  Supermarket(eventStore, doNothingBroker)
            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let added = superMarket.AddGood good
            Expect.isOk added "should be ok"

        testCase "add a good and set its quantity" <| fun _ ->
            setUp eventStore
            let superMarket =  Supermarket(eventStore, doNothingBroker)
            let id = Guid.NewGuid()
            let good = Good(id, "Good", 10.0m, [])
            let added = superMarket.AddGood good
            Expect.isOk added "should be ok"
            let changeQuantity = superMarket.SetGoodsQuantity (id, 10)
            Expect.isOk changeQuantity "should be ok"
            let retrievedQuantity = superMarket.GetGoodsQuantity id
            Expect.isOk retrievedQuantity "should be ok"
            let result = retrievedQuantity.OkValue
            Expect.equal result 10 "should be the same quantity"

        testCase "create a cart" <| fun _ ->
            setUp eventStore
            let superMarket =  Supermarket(eventStore, doNothingBroker)
            let cartId = Guid.NewGuid()
            let cart = Cart(cartId, Map.empty)
            let basket = superMarket.AddCart cart
            Expect.isOk basket "should be ok"

        testCase "add a good to the cart" <| fun _ ->
            setUp eventStore
            let supermarket = Supermarket(eventStore, doNothingBroker)
            let cartId = Guid.NewGuid()
            let cart = Cart(cartId, Map.empty)
            let cartAdded = supermarket.AddCart cart
            Expect.isOk cartAdded "should be ok"

            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let GoodAdded = supermarket.AddGood good
            Expect.isOk GoodAdded "should be ok"

            let addedToCart = supermarket.AddGoodToCart(cartId, good.Id, 10)
            Expect.isOk addedToCart "should be ok"

            let retrieved = supermarket.GetCart cartId
            Expect.isOk retrieved "should be ok"

            let quantityForGood = retrieved.OkValue.Goods.[good.Id]
            Expect.equal quantityForGood 10 "should be the same quantity"


    ]
    |> testSequenced
