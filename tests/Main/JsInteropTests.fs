[<Util.Testing.TestFixture>]
module Fable.Tests.JsInterop

open System
open Util.Testing
open Fable.Tests.Util

#if FABLE_COMPILER
open Fable.Core
open Fable.Core.JsInterop

[<Test>]
let ``Dynamic application works``() =
    let dynObj =
        createObj [
            "add" ==> Func<_,_,_>(fun x y -> x + y)
            "fn" ==> fun () ->
                createObj [
                    "subtract" ==> Func<_,_,_>(fun x y -> x - y)
                ]
            "child" ==>
                createObj [
                    "multiply" ==> Func<_,_,_>(fun x y -> x * y)
                ]
            "foo" ==> "foo"
            "apply" ==> fun (del: Func<int,int,int>) -> del.Invoke(2,3)
        ]
    dynObj?add(2,2) |> equal (box 4)
    // Assigning dynamic access result to a value
    let add = dynObj?add
    add(3,4) |> equal (box 7)
    // Accessing 2-level deep property
    dynObj?child?multiply(3,2) |> equal (box 6)
    // Dynamic application chaining
    dynObj?fn()?subtract(3,2) |> equal (box 1)
    // Using $ operator
    dynObj?add $ (2,2) |> equal (box 4)
    dynObj?foo |> unbox |> equal "foo"
    // Delegates are not modified when applied dynamically
    let del = Func<_,_,_>(fun x y -> y - x)
    dynObj?apply(del) |> unbox |> equal 1
    dynObj?apply(Func<_,_,_>(fun x y -> x - y)) |> unbox |> equal -1

let myMeth (x: int) (y: int) = x - y

[<Test>]
let ``Lambdas are converted to delegates with dynamic operators``() =
    let o =
        createObj [
            "bar" ==> fun x y z -> x * y * z
            "bar2" ==> myMeth
            "apply2and5" ==> fun (f: Func<int,int,int>) -> f.Invoke(2,5)
        ]
    o?bar(1,2,3) |> unbox<int> |> equal 6
    o?bar2(5,2) |> unbox<int> |> equal 3

    o?apply2and5(fun x y -> x + y) |> unbox<int> |> equal 7

    let f = unbox<obj> o?apply2and5
    f $ (fun x y -> x * y) |> unbox<int> |> equal 10

    o?foo <- fun x y -> x / y
    o?foo(25, 5) |> unbox<int> |> equal 5

type IMyOptions =
    interface end

type MyOptions =
    | Flag1
    | Name of string
    | [<CompiledName("Foo")>] QTY of int
    interface IMyOptions

type MyOptions2 =
    | Bar of int*int
    interface IMyOptions

[<Test>]
let ``KeyValueList works at compile time``() =
    let opts =
        [ Name "Fable" :> IMyOptions
        ; QTY 5 :> IMyOptions
        ; Flag1 :> IMyOptions
        ; Bar(2,3) :> IMyOptions ]
        |> keyValueList CaseRules.LowerFirst
    opts?name |> unbox |> equal "Fable"
    opts?foo |> unbox |> equal 5
    opts?flag1 |> unbox |> equal true
    opts?bar?(1) |> unbox |> equal 3
    let opts2 = keyValueList CaseRules.None [ Name "Fable"]
    opts2?Name |> unbox |> equal "Fable"

[<Test>]
let ``KeyValueList works at runtime``() =
    let buildAtRuntime = function
        | null | "" -> Flag1 :> IMyOptions
        | name -> Name name :> IMyOptions
    let opts =
        [ buildAtRuntime "Fable"
        ; QTY 5 :> IMyOptions
        ; Bar(2,3) :> IMyOptions
        ; buildAtRuntime ""]
        |> keyValueList CaseRules.LowerFirst
    opts?name |> unbox |> equal "Fable"
    opts?foo |> unbox |> equal 5
    opts?flag1 |> unbox |> equal true
    opts?bar?(0) |> unbox |> equal 2
    let opts2 = keyValueList CaseRules.None [ buildAtRuntime "Fable"]
    opts2?Name |> unbox |> equal "Fable"

type MyOptions3 =
    | Bar2 of int*int
    | Foo2 of MyOptions list

[<Test>]
let ``KeyValueList works with sublists``() =
    let opts1 =
        [ Bar2(2,3)
          Foo2 [ Name "Fable"
                 QTY 5
                 Flag1 ] ]
        |> keyValueList CaseRules.LowerFirst
    let opts2 =
        let subList =
            [ Name "Fábula"
              QTY 5
              Flag1 ]
        [ Bar2(2,3)
          Foo2 subList ]
        |> keyValueList CaseRules.LowerFirst
    opts1?foo2?name |> unbox |> equal "Fable"
    opts1?foo2?foo |> unbox |> equal 5
    opts1?foo2?flag1 |> unbox |> equal true
    opts1?bar2?(0) |> unbox |> equal 2
    opts2?foo2?name |> unbox |> equal "Fábula"
    opts2?foo2?foo |> unbox |> equal 5
    opts2?foo2?flag1 |> unbox |> equal true
    opts2?bar2?(1) |> unbox |> equal 3

type NameProp = { Name: string }
type Props =
    | Names of NameProp array
    | [<Erase>] Custom of key:string * value:obj

[<Test>]
let ``Array inside keyValueList is preserved`` () =
    let props = [ Names [| { Name = "name" } |] ]
    let actual = [ Names [| { Name = "name" } |] ] |> keyValueList CaseRules.LowerFirst |> toJson
    let expected = props |> keyValueList CaseRules.LowerFirst |> toJson
    actual |> equal expected

[<Test>]
let ``Erased union cases work with keyValueList`` () =
    let props: Props list = [ Custom("Foo", 5) ]
    let actual = [ Custom("Bar", 10) ] |> keyValueList CaseRules.LowerFirst
    let expected = props |> keyValueList CaseRules.LowerFirst
    !!actual?Bar |> equal 10
    !!expected?Foo |> equal 5

[<Test>]
let ``Dynamic casting works with keyValueList`` () =
    let props: Props list = [ !!("Foo", 5) ]
    let actual = [ (!!("Bar", 10): Props) ] |> keyValueList CaseRules.LowerFirst
    let expected = props |> keyValueList CaseRules.LowerFirst
    !!actual?Bar |> equal 10
    !!expected?Foo |> equal 5

let [<Emit("arguments.length")>] argCount: int = jsNative

type ArgCounter() =
    member x.foo() = argCount

[<Test>]
let ``Unit argument is not replaced by null in dynamic programming``() =
    let o = ArgCounter()
    !!o?foo() |> equal 0
    let f = box o?foo
    f$() |> unbox<int> |> equal 0
    !!(f :?> JsFunc).Invoke() |> equal 0

type IAdder =
    [<Emit("$1 + $2")>]
    abstract Add: int * int -> int

[<Erase>]
let adder: IAdder = jsNative

[<Test>]
let ``Erase attribute works``() =
    adder.Add(4, 5) |> equal 9

type TextStyle =
    [<Emit("\"foo\"")>]
    abstract Bar : string
    [<Emit("$1+$2")>]
    abstract Add : int*int -> int
    [<Emit("$0[$1]{{=$2}}")>]
    abstract Item : int->string option with get, set
    [<Emit("$0.fontFamily{{=$1}}")>]
    abstract FontFamily : string option with get, set
    [<Emit("{{$1?\"foo\":\"bar\"}}")>]
    abstract FontSize : bool -> string

[<Test>]
let ``Emit attribute works``() =
    let style = createEmpty<TextStyle>
    style.Bar |> equal "foo"
    style.Add(3,5) |> equal 8

[<Test>]
let ``Emit attribute conditional parameters works``() =
    let style = createEmpty<TextStyle>
    style.FontFamily <- Some "ha"
    style.FontFamily |> equal (Some "ha")
    !!style?fontFamily |> equal "ha"
    style.[5] <- Some "ho"
    style.[5] |> equal (Some "ho")
    style.[10] |> equal None

[<Test>]
let ``Emit attribute conditional syntax works``() =
    let style = createEmpty<TextStyle>
    style.FontSize(true) |> equal "foo"
    style.FontSize(false) |> equal "bar"


type InnerRecord = {
    Float: float
}

type Record = {
    String: string
    Int: int
    InnerRecord: InnerRecord
}

[<Test>]
let ``nameof works``() =
    let record = { String = ""; Int = 0; InnerRecord = { Float = 5.0 } }
    nameof(record) |> equal "record"
    nameof(record.String) |> equal "String"
    nameof(record.Int) |> equal "Int"
    nameof(record.InnerRecord) + "." + nameof(record.InnerRecord.Float)
    |> equal "InnerRecord.Float"
    nameof(typeof<Record>) |> equal "Record"
    nameof(typeof<InnerRecord>) |> equal "InnerRecord"

[<Test>]
let ``nameofLambda works``() =
    nameofLambda(fun (x:Record) -> x.String) |> equal "String"
    nameofLambda(fun (x:Record) -> x.Int) |> equal "Int"

[<StringEnum>]
type MyStrings =
    | Vertical
    | [<CompiledName("Horizontal")>] Horizontal

[<Test>]
let ``StringEnum attribute works``() =
    Vertical |> unbox |> equal "vertical"
    Horizontal |> unbox |> equal "Horizontal"

[<StringEnum>]
#endif
type Field = OldPassword | NewPassword | ConfirmPassword

let validatePassword = function
    | OldPassword -> "op"
    | NewPassword -> "np"
    | ConfirmPassword -> "cp"

[<Test>]
let ``Pattern matching with StringEnum works``() =
    validatePassword NewPassword
    |> equal "np"
