namespace Fable.Core
open System

[<AutoOpen>]
module Exceptions =
    /// Used to indicate that a member is only implemented in native Javascript
    let jsNative<'T> : 'T = failwith "JS only"

/// Used for erased union types and to ignore modules in JS compilation.
/// More info: http://fable.io/docs/interacting.html#Erase-attribute
type EraseAttribute() =
    inherit Attribute()

/// The module, type, function... is globally accessible in JS.
/// More info: http://fable.io/docs/interacting.html#Import-attribute
type GlobalAttribute() =
    inherit Attribute()
    new (name: string) = GlobalAttribute()

/// References to the module, type, function... will be replaced by import statements.
/// Use `[<Import("default", "my-package")>] to import the default member.
/// Use `[<Import("*", "my-package")>] to import the whole package.
/// More info: http://fable.io/docs/interacting.html#Import-attribute
type ImportAttribute(selector: string, from: string) =
    inherit Attribute()

/// Function calls will be replaced by inlined JS code.
/// More info: http://fable.io/docs/interacting.html#Import-attribute
type EmitAttribute private () =
    inherit Attribute()
    new (macro: string) = EmitAttribute()
    new (emitterType: Type, methodName: string) = EmitAttribute()
    new (emitterType: Type, methodName: string, extraArg: string) = EmitAttribute()

/// When this is attached to a method, Fable will add the generic info
/// as an extra argument to every call, making it possible to access
/// a type 'T with `typeof<'T>` within the method body
[<AttributeUsage(AttributeTargets.Method)>]
type PassGenericsAttribute() =
    inherit Attribute()

/// Compile a record as a JS object literals.
/// More info: http://fable.io/docs/interacting.html
type PojoAttribute() =
    inherit Attribute()

/// Compile union types as string literals.
/// More info: http://fable.io/docs/interacting.html#StringEnum-attribute
[<AttributeUsage(AttributeTargets.Class)>]
type StringEnumAttribute() =
    inherit Attribute()

/// Erased union type to represent one of two possible values.
/// More info: http://fable.io/docs/interacting.html#Erase-attribute
type [<Erase>] U2<'a, 'b> =
    | Case1 of 'a
    | Case2 of 'b
    static member op_ErasedCast(x:'a) = Case1 x
    static member op_ErasedCast(x:'b) = Case2 x

/// Erased union type to represent one of three possible values.
/// More info: http://fable.io/docs/interacting.html#Erase-attribute
type [<Erase>] U3<'a, 'b, 'c> =
    | Case1 of 'a
    | Case2 of 'b
    | Case3 of 'c
    static member op_ErasedCast(x:'a) = Case1 x
    static member op_ErasedCast(x:'b) = Case2 x
    static member op_ErasedCast(x:'c) = Case3 x

/// Erased union type to represent one of four possible values.
/// More info: http://fable.io/docs/interacting.html#Erase-attribute
type [<Erase>] U4<'a, 'b, 'c, 'd> =
    | Case1 of 'a
    | Case2 of 'b
    | Case3 of 'c
    | Case4 of 'd
    static member op_ErasedCast(x:'a) = Case1 x
    static member op_ErasedCast(x:'b) = Case2 x
    static member op_ErasedCast(x:'c) = Case3 x
    static member op_ErasedCast(x:'d) = Case4 x

/// Erased union type to represent one of five possible values.
/// More info: http://fable.io/docs/interacting.html#Erase-attribute
type [<Erase>] U5<'a, 'b, 'c, 'd, 'e> =
    | Case1 of 'a
    | Case2 of 'b
    | Case3 of 'c
    | Case4 of 'd
    | Case5 of 'e
    static member op_ErasedCast(x:'a) = Case1 x
    static member op_ErasedCast(x:'b) = Case2 x
    static member op_ErasedCast(x:'c) = Case3 x
    static member op_ErasedCast(x:'d) = Case4 x
    static member op_ErasedCast(x:'e) = Case5 x

/// Erased union type to represent one of six possible values.
/// More info: http://fable.io/docs/interacting.html#Erase-attribute
type [<Erase>] U6<'a, 'b, 'c, 'd, 'e, 'f> =
    | Case1 of 'a
    | Case2 of 'b
    | Case3 of 'c
    | Case4 of 'd
    | Case5 of 'e
    | Case6 of 'f
    static member op_ErasedCast(x:'a) = Case1 x
    static member op_ErasedCast(x:'b) = Case2 x
    static member op_ErasedCast(x:'c) = Case3 x
    static member op_ErasedCast(x:'d) = Case4 x
    static member op_ErasedCast(x:'e) = Case5 x
    static member op_ErasedCast(x:'f) = Case6 x

/// DO NOT USE: Internal type for Fable dynamic operations
type Applicable = obj->obj

type CaseRules =
    | None = 0
    | LowerFirst = 1

module Testing =
    type TestAttribute() =
        inherit Attribute()

    type TestFixtureAttribute() =
        inherit Attribute()

    type TestFixtureSetUpAttribute() =
        inherit Attribute()

    type TestFixtureTearDownAttribute() =
        inherit Attribute()

    type SetUpAttribute() =
        inherit Attribute()

    type TearDownAttribute() =
        inherit Attribute()

    type Assert =
        static member AreEqual(expected: 'T, actual: 'T, ?msg: string): unit = jsNative

