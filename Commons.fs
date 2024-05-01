
namespace ShoppingCart

open System
open Sharpino
open Sharpino.Storage
open Sharpino.Core
open Sharpino.Lib.Core.Commons
open Sharpino.Utils
open Sharpino.Core
open Sharpino.Utils
open Sharpino.Result
open MBrace.FsPickler.Json
open MBrace.FsPickler

module Commons =

    type MySerializer<'F> =
        abstract member Deserialize<'A> : 'F -> Result<'A, string>
        abstract member Serialize<'A> : 'A -> 'F

    let jsonPickler = FsPickler.CreateJsonSerializer(indent = false)
    let binaryPickle = FsPickler.CreateBinarySerializer()

    let jsonPicklerSerializer =
        { new MySerializer<string> with
            member this.Deserialize<'A> json =
                try
                    jsonPickler.UnPickleOfString<'A> json |> Ok
                with
                | ex -> Error ex.Message
            member this.Serialize<'A> (obj: 'A) =
                jsonPickler.PickleToString obj
        }
    
    let binaryPicklerSerializer = 
        { new MySerializer<byte[]> with
            member this.Deserialize<'A> (bytes: byte[]) =
                try
                    binaryPickle.UnPickle<'A> bytes |> Ok
                with
                | ex -> Error ex.Message
            member this.Serialize<'A> (obj: 'A) =
                binaryPickle.Pickle obj
        }

    let mutable globalSerializer: MySerializer<_> = jsonPicklerSerializer
    let mutable globalSerializerJson: MySerializer<_> = binaryPicklerSerializer

    type MyGlobalSerializer(mySerializer: MySerializer<_>) =
        let mutable mySerializer = mySerializer

        member this.SetSerializer (serializer: MySerializer<_>) =
            mySerializer <- serializer

        member this.Deserialize (json: 'F) =
            mySerializer.Deserialize json

        member this.Serialize (obj: 'A) =
            mySerializer.Serialize obj
