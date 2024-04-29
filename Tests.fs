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
open Sharpino.TestUtils

[<Tests>]
let tests =
    let setUp (eventStore: IEventStore) =
        eventStore.Reset GoodsContainer.Version GoodsContainer.StorageName
        eventStore.Reset Good.Version Good.StorageName
        eventStore.ResetAggregateStream Good.Version Good.StorageName

        eventStore.Reset Cart.Version Cart.StorageName
        eventStore.ResetAggregateStream Cart.Version Cart.StorageName

    let connection = 
            "Server=127.0.0.1;" +
            "Database=es_shopping_cart;" +
            "User Id=safe;"+
            "Password=safe;"

    let eventStoreMemory = MemoryStorage():> IEventStore
    let eventStorePostgres = PgEventStore(connection):> IEventStore

    let marketInstances =
        [
            Supermarket(eventStorePostgres, doNothingBroker), eventStorePostgres, "Postgres";
            Supermarket(eventStoreMemory, doNothingBroker), eventStoreMemory, "Memory";
        ]

    testList "samples" [
        multipleTestCase "there are no good in a Supermarket" marketInstances <| fun (supermarket, eventStore, _) ->
            setUp eventStore
            // let supermarket = fsupermarket()

            let goods = supermarket.Goods

            Expect.isOk goods "should be ok"
            Expect.equal goods.OkValue [] "There are no goods in the supermarket."
        
        multipleTestCase "add a good to the supermarket and retrieve it" marketInstances <| fun (supermarket, eventStore, _) ->
            setUp eventStore
            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let added = supermarket.AddGood good
            Expect.isOk added "should be ok"
            let retrieved = supermarket.GetGood good.Id
            Expect.isOk retrieved "should be ok"
            let retrieved' = retrieved.OkValue
            Expect.equal  retrieved'.Id good.Id "should be the same good"

        multipleTestCase "add a good and put it in the supermarket" marketInstances <| fun (supermarket, eventStore, _) ->
            setUp eventStore
            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let added = supermarket.AddGood good
            Expect.isOk added "should be ok"

        multipleTestCase "add a good and its quantity is zero" marketInstances <| fun (supermarket, eventStore, _) ->
            setUp eventStore
            let id = Guid.NewGuid()
            let good = Good(id, "Good", 10.0m, [])
            let added = supermarket.AddGood good
            Expect.isOk added "should be ok"
            let retrievedQuantity = supermarket.GetGoodsQuantity id
            Expect.isOk retrievedQuantity "should be ok"
            let result = retrievedQuantity.OkValue
            Expect.equal result 0 "should be the same quantity"

        multipleTestCase "add a good, then increase its quantity - Ok" marketInstances <| fun (supermarket, eventStore, _) ->
            setUp eventStore
            let id = Guid.NewGuid()
            let good = Good(id, "Good", 10.0m, [])
            let added = supermarket.AddGood good
            Expect.isOk added "should be ok"
            let setQuantity = supermarket.AddQuantity(id, 10)
            Expect.isOk setQuantity "should be ok"
            let retrievedQuantity = supermarket.GetGoodsQuantity id
            Expect.isOk retrievedQuantity "should be ok"
            let result = retrievedQuantity.OkValue
            Expect.equal result 10 "should be the same quantity"


        multipleTestCase "create a cart" marketInstances <| fun (supermarket, eventStore, _) ->
            setUp eventStore
            let cartId = Guid.NewGuid()
            let cart = Cart(cartId, Map.empty)
            let basket = supermarket.AddCart cart
            Expect.isOk basket "should be ok"

        multipleTestCase "add a good, add a quantity and then put something in a cart. the total quantity will be decreased - Ok" marketInstances <| fun (supermarket, eventStore, _) ->
            setUp eventStorePostgres
            let supermarket = Supermarket(eventStorePostgres, doNothingBroker)
            let cartId = Guid.NewGuid()
            let cart = Cart(cartId, Map.empty)
            let cartAdded = supermarket.AddCart cart
            Expect.isOk cartAdded "should be ok"

            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let GoodAdded = supermarket.AddGood good
            Expect.isOk GoodAdded "should be ok"

            let addQuantity = supermarket.AddQuantity(good.Id, 10)

            let addedToCart = supermarket.AddGoodToCart(cartId, good.Id, 1)
            Expect.isOk addedToCart "should be ok"

            let retrieved = supermarket.GetCart cartId
            Expect.isOk retrieved "should be ok"

            let quantityForGood = retrieved.OkValue.Goods.[good.Id]
            Expect.equal quantityForGood 1 "should be the same quantity"

            let quantity = supermarket.GetGoodsQuantity good.Id
            Expect.isOk quantity "should be ok"

            let result = quantity.OkValue
            Expect.equal result 9 "should be the same quantity"


        multipleTestCase "add and a good and it's quantity will be zero - Ok" marketInstances <| fun (supermarket, eventStore, _) ->
            setUp eventStore
            let cartId = Guid.NewGuid()
            let cart = Cart(cartId, Map.empty)
            let cartAdded = supermarket.AddCart cart
            Expect.isOk cartAdded "should be ok"

            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let GoodAdded = supermarket.AddGood good
            Expect.isOk GoodAdded "should be ok"
            let quantity = supermarket.GetGoodsQuantity good.Id
            Expect.isOk quantity "should be ok"

            let result = quantity.OkValue
            Expect.equal result 0 "should be the same quantity"

        multipleTestCase "can't add twice a good with the same name - Error" marketInstances <| fun (supermarket, eventStore, text) ->
            setUp eventStore
            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let added = supermarket.AddGood good
            Expect.isOk added "should be ok"

            let good2 = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let addedTwice = supermarket.AddGood good2
            Expect.isError addedTwice "should be an error"

        multipleTestCase "add a good and remove it - Ok" marketInstances <| fun (supermarket, eventStore, _) ->
            setUp eventStore
            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let added = supermarket.AddGood good
            Expect.isOk added "should be ok"
            let removed = supermarket.RemoveGood good.Id
            Expect.isOk removed "should be ok"

            let retrieved = supermarket.GetGood good.Id
            Expect.isError retrieved "should be an error"

        multipleTestCase  "when remove a good then can gets its quantity - Error" marketInstances <| fun (supermarket, eventStore, _) ->
            setUp eventStore
            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let added = supermarket.AddGood good
            Expect.isOk added "should be ok"
            let removed = supermarket.RemoveGood good.Id
            Expect.isOk removed "should be ok"

            let quantity = supermarket.GetGoodsQuantity good.Id
            Expect.isError quantity "should be an error"


    ]
    |> testSequenced
