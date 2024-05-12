module Tests

open ShoppingCart.Good
open ShoppingCart.GoodsContainer
open ShoppingCart.Supermarket
open ShoppingCart.Cart
open System
open Sharpino.Storage
open Sharpino.PgStorage
open Sharpino.TestUtils
open Sharpino.PgBinaryStore
open Sharpino.MemoryStorage
open Expecto
open Sharpino.KafkaBroker
open Sharpino.Cache

[<Tests>]
let tests =

    let setUp (eventStore: IEventStore<'F>) =
        eventStore.Reset GoodsContainer.Version GoodsContainer.StorageName
        eventStore.Reset Good.Version Good.StorageName
        eventStore.ResetAggregateStream Good.Version Good.StorageName

        eventStore.Reset Cart.Version Cart.StorageName
        eventStore.ResetAggregateStream Cart.Version Cart.StorageName

        AggregateCache<Cart, byte[]>.Instance.Clear()
        AggregateCache<Good, byte[]>.Instance.Clear()
        StateCache<GoodsContainer>.Instance.Clear()

    let connection = 
            "Server=127.0.0.1;" +
            "Database=es_shopping_cart;" +
            "User Id=safe;"+
            "Password=safe;"

    let byteAConnection =
            "Server=127.0.0.1;" +
            "Database=es_shopping_cart_bin;" +
            "User Id=safe;"+
            "Password=safe;"

    // let eventStoreMemory = MemoryStorage() //:> IEventStore<string>
    // let eventStorePostgres = PgEventStore(connection) //:> IEventStore<string>
    let eventStorePostgresBin = PgBinaryStore(byteAConnection) :> IEventStore<byte[]>
    let eventBroker = getKafkaBroker("localhost:9092")

    let marketInstances =
        [
            // can't use the "multiple tests" feature as there are insufficient genericity at the moment
            // Supermarket(eventStorePostgres, doNothingBroker), "eventStorePostgres", fun () -> setUp eventStorePostgres ;
            // Supermarket(eventStoreMemory, doNothingBroker), "eventStoreMemory", fun () -> setUp eventStoreMemory; 
            Supermarket(eventStorePostgresBin, doNothingBroker), "eventStorePostgresBinary", fun () -> setUp eventStorePostgresBin ; 
        ]

    testList "samples" [

        multipleTestCase "there are no good in a Supermarket" marketInstances <| fun (supermarket, eventStore, setup ) ->
            setup()

            let goods = supermarket.Goods

            Expect.isOk goods "should be ok"
            Expect.equal goods.OkValue [] "There are no goods in the supermarket."
        
        multipleTestCase "add a good to the supermarket and retrieve it" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()

            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let added = supermarket.AddGood good
            Expect.isOk added "should be ok"
            let retrieved = supermarket.GetGood good.Id
            Expect.isOk retrieved "should be ok"
            let retrieved' = retrieved.OkValue
            Expect.equal  retrieved'.Id good.Id "should be the same good"

        multipleTestCase "add a good and put it in the supermarket" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()

            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let added = supermarket.AddGood good
            Expect.isOk added "should be ok"

        multipleTestCase "add a good and its quantity is zero" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()

            let id = Guid.NewGuid()
            let good = Good(id, "Good", 10.0m, [])
            let added = supermarket.AddGood good
            Expect.isOk added "should be ok"
            let retrievedQuantity = supermarket.GetGoodsQuantity id
            Expect.isOk retrievedQuantity "should be ok"
            let result = retrievedQuantity.OkValue
            Expect.equal result 0 "should be the same quantity"

        multipleTestCase "add a good, then increase its quantity - Ok" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()

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


        multipleTestCase "create a cart" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()
            let cartId = Guid.NewGuid()
            let cart = Cart(cartId, Map.empty)
            let basket = supermarket.AddCart cart
            Expect.isOk basket "should be ok"

        multipleTestCase "add a good, add a quantity and then put something in a cart. the total quantity will be decreased - Ok" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()

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

        multipleTestCase "try adding more items than available. - Error" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()

            let cartId = Guid.NewGuid()
            let cart = Cart(cartId, Map.empty)
            let cartAdded = supermarket.AddCart cart
            Expect.isOk cartAdded "should be ok"

            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let GoodAdded = supermarket.AddGood good
            Expect.isOk GoodAdded "should be ok"

            let addQuantity = supermarket.AddQuantity(good.Id, 10)

            let addedToCart = supermarket.AddGoodToCart(cartId, good.Id, 11)
            Expect.isError addedToCart "should be an error"

        multipleTestCase "try adding a good into an unexisting cart - Error" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()

            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let GoodAdded = supermarket.AddGood good
            Expect.isOk GoodAdded "should be ok"

            let addedToCart = supermarket.AddGoodToCart(Guid.NewGuid(), good.Id, 1)
            Expect.isError addedToCart "should be an error"

        multipleTestCase "try adding an unexisting good to a cart - Error" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()

            let cartId = Guid.NewGuid()
            let cart = Cart(cartId, Map.empty)
            let cartAdded = supermarket.AddCart cart
            Expect.isOk cartAdded "should be ok"

            let addedToCart = supermarket.AddGoodToCart(cartId, Guid.NewGuid(), 1)
            Expect.isError addedToCart "should be an error" 

        multipleTestCase "add multiple goods to a cart - Ok" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()

            let cartId = Guid.NewGuid()
            let cart = Cart(cartId, Map.empty)
            let cartAdded = supermarket.AddCart cart
            Expect.isOk cartAdded "should be ok"

            let good1 = Good(Guid.NewGuid(), "Good1", 10.0m, [])
            let GoodAdded1 = supermarket.AddGood good1
            Expect.isOk GoodAdded1 "should be ok"

            let good2 = Good(Guid.NewGuid(), "Good2", 10.0m, [])
            let GoodAdded2 = supermarket.AddGood good2
            Expect.isOk GoodAdded2 "should be ok"

            let _ = supermarket.AddQuantity(good1.Id, 10)
            let _ = supermarket.AddQuantity(good2.Id, 10)

            let addedToCart1 = supermarket.AddGoodsToCart(cartId, [(good1.Id, 1); (good2.Id, 1)])

            let cart = supermarket.GetCart cartId
            Expect.isOk cart "should be ok"

            let result = cart.OkValue.Goods
            Expect.equal result.Count 2 "should be the same quantity"  

            Expect.equal result.[good1.Id] 1 "should be the same quantity"
            Expect.equal result.[good2.Id] 1 "should be the same quantity"

            let good1Quantity = supermarket.GetGoodsQuantity good1.Id
            Expect.isOk good1Quantity "should be ok"
            Expect.equal good1Quantity.OkValue 9 "should be the same quantity"

            let Good2Quantity = supermarket.GetGoodsQuantity good2.Id
            Expect.isOk Good2Quantity "should be ok"
            Expect.equal Good2Quantity.OkValue 9 "should be the same quantity"

        multipleTestCase "add multiple good to a cart, exceeding quantity of one - Error" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()

            let cartId = Guid.NewGuid()

            let cartId = Guid.NewGuid()
            let cart = Cart(cartId, Map.empty)
            let cartAdded = supermarket.AddCart cart
            Expect.isOk cartAdded "should be ok"

            let good1 = Good(Guid.NewGuid(), "Good1", 10.0m, [])
            let GoodAdded1 = supermarket.AddGood good1
            Expect.isOk GoodAdded1 "should be ok"

            let good2 = Good(Guid.NewGuid(), "Good2", 10.0m, [])
            let GoodAdded2 = supermarket.AddGood good2
            Expect.isOk GoodAdded2 "should be ok"

            let _ = supermarket.AddQuantity(good1.Id, 10)
            let _ = supermarket.AddQuantity(good2.Id, 10)

            let addedToCart1 = supermarket.AddGoodsToCart(cartId, [(good1.Id, 11); (good2.Id, 1)])
            
            Expect.isError addedToCart1 "should be an error"

            let cart = supermarket.GetCart cartId
            Expect.isOk cart "should be ok"
            Expect.equal cart.OkValue.Goods.Count 0 "should be the same quantity"   

            let retrievedGood1 = supermarket.GetGoodsQuantity good1.Id
            Expect.isOk retrievedGood1 "should be ok"

            let result1 = retrievedGood1.OkValue
            Expect.equal result1 10 "should be the same quantity"

            let result2 = supermarket.GetGoodsQuantity good2.Id
            Expect.isOk result2 "should be ok"
            Expect.equal result2.OkValue 10 "should be the same quantity"

        multipleTestCase "add and a good and it's quantity will be zero - Ok" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()

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

        multipleTestCase "can't add twice a good with the same name - Error" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup()

            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let added = supermarket.AddGood good
            Expect.isOk added "should be ok"

            let good2 = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let addedTwice = supermarket.AddGood good2
            Expect.isError addedTwice "should be an error"

        multipleTestCase "add a good and remove it - Ok" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()

            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let added = supermarket.AddGood good
            Expect.isOk added "should be ok"
            let removed = supermarket.RemoveGood good.Id
            Expect.isOk removed "should be ok"

            let retrieved = supermarket.GetGood good.Id
            Expect.isError retrieved "should be an error"

        multipleTestCase  "when remove a good then can gets its quantity - Error" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()
            let good = Good(Guid.NewGuid(), "Good", 10.0m, [])
            let added = supermarket.AddGood good
            Expect.isOk added "should be ok"
            let removed = supermarket.RemoveGood good.Id
            Expect.isOk removed "should be ok"

            let quantity = supermarket.GetGoodsQuantity good.Id
            Expect.isError quantity "should be an error"

    ]
    |> testSequenced
