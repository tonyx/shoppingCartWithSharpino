
module KafkaTests

open ShoppingCart.Good
open ShoppingCart.Commons
open ShoppingCart.GoodsContainer
open ShoppingCart.Supermarket
open ShoppingCart.Cart
open System
open Sharpino.Storage
open Sharpino.Core
open Sharpino.KafkaReceiver
open Sharpino.PgStorage
open Sharpino.KafkaBroker
open Sharpino.TestUtils
open Sharpino.PgBinaryStore
open Sharpino.MemoryStorage
open Expecto
open Confluent.Kafka
open FsKafka
open Tests
open ShoppingCart.CartEvents
open ShoppingCart.GoodEvents
open FsToolkit.ErrorHandling


let getFromMessage<'E> value =
    result {
        let! okBinaryDecoded = getStrAggregateMessage value
        let message = okBinaryDecoded.BrokerEvent
        let actual = 
            match message with
                | StrEvent x -> jsonPicklerSerializer.Deserialize<'E> x |> Result.get
                | BinaryEvent x -> binPicklerSerializer.Deserialize<'E> x |> Result.get
        return actual
    }

[<Tests>]
let kafkaTests =
    testList "Supermarket" [
        multipleTestCase "add a good to a cart, and verify events are published on the cart and on the good side - Ok" marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()

            let cartId = Guid.NewGuid()
            printf "cart id1: %A \n" cartId
            let iniCart = Cart(cartId, Map.empty)
            let cartAdded = supermarket.AddCart iniCart
            Expect.isOk cartAdded "should be ok"

            let good1 = Good(Guid.NewGuid(), "Good1", 10.0m, [])
            printf "good1 id: %A \n" good1.Id
            let GoodAdded1 = supermarket.AddGood good1
            Expect.isOk GoodAdded1 "should be ok"

            let _ = supermarket.AddQuantity(good1.Id, 8)

            let addedToCart1 = supermarket.AddGoodsToCart(cartId, [(good1.Id, 1)])

            let cart = supermarket.GetCart cartId
            Expect.isOk cart "should be ok"

            let result = cart.OkValue.Goods
            Expect.equal result.Count 1 "should be the same quantity"  

            Expect.equal result.[good1.Id] 1 "should be the same quantity"

            let good1Quantity = supermarket.GetGoodsQuantity good1.Id
            Expect.isOk good1Quantity "should be ok"
            Expect.equal good1Quantity.OkValue 7 "should be the same quantity"

            let topic = (Good.StorageName + "-" + Good.Version).Replace("_", "")
            printf "topic %s\n" topic

            let goodConsumer = ConsumerX<Good, GoodEvents>([good1], topic, "MyClientIdX", "localhost:9092", "MyGroupIdX", 4000)
            
            goodConsumer.Consuming()

            let binaryDecoded = getStrAggregateMessage goodConsumer.GMessages.[0].Message.Value

            // let okBinaryDecoded = goodConsumer.GetProcessedMessages |> Result.get |> List.head

            Expect.isOk binaryDecoded "should be ok"
            let okBinaryDecoded = binaryDecoded.OkValue
            Expect.equal okBinaryDecoded.AggregateId good1.Id "should be the same id"

            let binaryDecodeds = goodConsumer.GetMessages |> Result.get
            Expect.equal binaryDecodeds.[0].AggregateId good1.Id "should be the same id"

            let cartTopic = (Cart.StorageName + "-" + Cart.Version).Replace("_", "")
            let cartConsumer = ConsumerX<Cart, CartEvents>([iniCart], cartTopic, "MyClientIdX", "localhost:9092", "MyGroupIdX", 4000)

            cartConsumer.Consuming()

            let binaryDecodedCartMessage = getStrAggregateMessage cartConsumer.GMessages.[0].Message.Value
            Expect.isOk binaryDecodedCartMessage "should be ok"
            let okBinaryDecodedCartMessage = binaryDecodedCartMessage.OkValue
            Expect.equal okBinaryDecodedCartMessage.AggregateId cartId "should be the same id"

        multipleTestCase "add a good to a cart and  decript the events - Ok"  marketInstances <| fun (supermarket, eventStore, setup) ->
            setup ()
            
            // prepare the good adding it to the cart
            let cartId = Guid.NewGuid ()
            let cart = Cart (cartId, Map.empty)
            let cartAdded = supermarket.AddCart cart
            Expect.isOk cartAdded "should be ok"

            let good1 = Good (Guid.NewGuid(), "Good1", 10.0m, [])
            let GoodAdded1 = supermarket.AddGood good1
            Expect.isOk GoodAdded1 "should be ok"

            let addToSupermarket = supermarket.AddQuantity (good1.Id, 8)
            Expect.isOk addToSupermarket "should be ok"

            let addedToCart1 = supermarket.AddGoodToCart (cartId, good1.Id, 1)
            Expect.isOk addedToCart1 "should be ok"

            // now verify that the events are published on the good and cart side
            let topic = (Good.StorageName + "-" + Good.Version).Replace("_", "")
            let goodConsumer = ConsumerX<Good, GoodEvents>([good1], topic, "MyClientIdX", "localhost:9092", "MyGroupIdX", 4000)

            goodConsumer.Consuming()

            let actualGoodEvents = goodConsumer.GetEvents |> Result.get |> Set.ofList

            let expected = GoodEvents.QuantityAdded 8
            let expected2 = GoodEvents.QuantityRemoved 1

            let expected = Set.ofList [expected; expected2]


            Expect.equal actualGoodEvents expected "should be the same event"

            let cartTopic = (Cart.StorageName + "-" + Cart.Version).Replace("_", "")
            let cartConsumer = ConsumerX<Cart, CartEvents> ([cart], cartTopic, "MyClientIdX", "localhost:9092", "MyGroupIdX", 4000)

            cartConsumer.Consuming()

            let expected = CartEvents.GoodAdded (good1.Id, 1)

            let actual = cartConsumer.GetEvents |> Result.get |> List.head

            Expect.equal actual expected "should be the same event"

        fmultipleTestCase "add two goods into a card - Ok" marketInstances <| fun (supermarket, eventStore, setup) ->

            setup ()

            let cartId = Guid.NewGuid ()
            let cart = Cart (cartId, Map.empty)
            let cartAdded = supermarket.AddCart cart
            Expect.isOk cartAdded "should be ok"

            let good1 = Good (Guid.NewGuid(), "Good1", 10.0m, [])
            let GoodAdded1 = supermarket.AddGood good1
            Expect.isOk GoodAdded1 "should be ok"

            let good2 = Good (Guid.NewGuid(), "Good2", 20.0m, [])
            let GoodAdded2 = supermarket.AddGood good2
            Expect.isOk GoodAdded2 "should be ok"

            let addToSupermarket1 = supermarket.AddQuantity (good1.Id, 8)
            Expect.isOk addToSupermarket1 "should be ok"

            let addToSupermarket2 = supermarket.AddQuantity (good2.Id, 5)
            Expect.isOk addToSupermarket2 "should be ok"

            let addedToCart1 = supermarket.AddGoodsToCart (cartId, [(good1.Id, 2); (good2.Id, 1)])
            Expect.isOk addedToCart1 "should be ok"

            let topic = (Good.StorageName + "-" + Good.Version).Replace("_", "")

            let consumer = ConsumerX<Good, GoodEvents> ([good1], topic, "MyClientIdX", "localhost:9092", "MyGroupIdX", 4000)
            consumer.Consuming()

            let actuals = consumer.GetEvents |> Result.get |> Set.ofList
            let expected1 = GoodEvents.QuantityAdded 8
            let expected2 = GoodEvents.QuantityAdded 5
            let expected3 = GoodEvents.QuantityRemoved 2
            let expected4 = GoodEvents.QuantityRemoved 1

            let expected = Set.ofList [expected1; expected2; expected3; expected4]
            Expect.equal actuals expected "should be the same events"

            let cartTopic = (Cart.StorageName + "-" + Cart.Version).Replace("_", "")
            let cartConsumer = ConsumerX<Cart, CartEvents> ([cart], cartTopic, "MyClientIdX", "localhost:9092", "MyGroupIdX", 4000)
            cartConsumer.Consuming()

            let expected1 = CartEvents.GoodAdded (good1.Id, 2)
            let expected2 = CartEvents.GoodAdded (good2.Id, 1)
            let expecteds = [expected1; expected2] |> Set.ofList

            let actuals = cartConsumer.GetEvents |> Result.get |> Set.ofList
            Expect.equal expecteds actuals "should be the same events"

        fmultipleTestCase "add one good twice and another good once, get related events by the consumer - Ok" marketInstances <| fun (supermarket, eventStore, setup) ->

            setup ()

            let cartId = Guid.NewGuid ()
            let cart = Cart (cartId, Map.empty)
            let cartAdded = supermarket.AddCart cart
            Expect.isOk cartAdded "should be ok"

            let good1Id = Guid.NewGuid()
            let good1 = Good (good1Id, "Good1", 10.0m, [])
            let GoodAdded1 = supermarket.AddGood good1
            Expect.isOk GoodAdded1 "should be ok"

            let good2Id = Guid.NewGuid()
            let good2 = Good (good2Id, "Good2", 20.0m, [])
            let GoodAdded2 = supermarket.AddGood good2
            Expect.isOk GoodAdded2 "should be ok"

            let addToSupermarket1 = supermarket.AddQuantity (good1.Id, 8)
            Expect.isOk addToSupermarket1 "should be ok"

            let addToSupermarket12 = supermarket.AddQuantity (good1.Id, 13)
            Expect.isOk addToSupermarket1 "should be ok"

            let addToSupermarket2 = supermarket.AddQuantity (good2.Id, 5)
            Expect.isOk addToSupermarket2 "should be ok"

            let expected1 = [GoodEvents.QuantityAdded 8; GoodEvents.QuantityAdded 13]
            let expected2 = [GoodEvents.QuantityAdded 5]

            let topic = (Good.StorageName + "-" + Good.Version).Replace("_", "")
            let consumer = ConsumerX<Good, GoodEvents> ([good1], topic, "MyClientIdX", "localhost:9092", "MyGroupIdX", 4000)

            consumer.Consuming()

            let actual1 = consumer.GetEventsByAggregate good1Id |> Result.get 
            Expect.equal actual1 expected1 "should be the same events"
            let actual2 = consumer.GetEventsByAggregate good2Id |> Result.get
            Expect.equal actual2 expected2 "should be the same events"

        fmultipleTestCase "add one good twice and another good once, get related events by the consumer. Compute the evolve - Ok" marketInstances <| fun (supermarket, eventStore, setup) ->

            setup ()

            let cartId = Guid.NewGuid ()
            let cart = Cart (cartId, Map.empty)
            let cartAdded = supermarket.AddCart cart
            Expect.isOk cartAdded "should be ok"

            let good1Id = Guid.NewGuid()
            let good1 = Good (good1Id, "Good1", 10.0m, [])
            let GoodAdded1 = supermarket.AddGood good1
            Expect.isOk GoodAdded1 "should be ok"

            let good2Id = Guid.NewGuid()
            let good2 = Good (good2Id, "Good2", 20.0m, [])
            let GoodAdded2 = supermarket.AddGood good2
            Expect.isOk GoodAdded2 "should be ok"

            let addToSupermarket1 = supermarket.AddQuantity (good1.Id, 9)
            Expect.isOk addToSupermarket1 "should be ok"

            let addToSupermarket12 = supermarket.AddQuantity (good1.Id, 11)
            Expect.isOk addToSupermarket12 "should be ok"

            let addToSupermarket13 = supermarket.AddQuantity (good1.Id, 17)
            Expect.isOk addToSupermarket13 "should be ok"

            let topic = (Good.StorageName + "-" + Good.Version).Replace("_", "")
            let consumer = ConsumerX<Good, GoodEvents> ([good1], topic, "MyClientIdQ", "localhost:9092", "MyGroupIdQ", 4000)

            let expected1 = [GoodEvents.QuantityAdded 9; GoodEvents.QuantityAdded 11; GoodEvents.QuantityAdded 17]

            consumer.Consuming()

            let actual1 = consumer.GetEventsByAggregate good1Id |> Result.get 
            Expect.equal actual1 expected1 "should be the same events"

            let good1State = actual1 |> evolve good1 |> Result.get
            let actualGood1State = supermarket.GetGood good1Id |> Result.get
            Expect.equal good1State.Quantity actualGood1State.Quantity "should be the same state"

    ]
    |> testSequenced