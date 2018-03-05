namespace Fable.Plugins

open Fable
open Fable.AST
open Fable.AST.Babel
open Fable.Fable2Babel

module Util =
    let (|TestFixture|_|) (decl: Fable.Declaration) =
        match decl with
        | Fable.EntityDeclaration (ent, _, _, entDecls, entRange) ->
            match ent.TryGetDecorator "TestFixture" with
            | Some _ -> Some (ent, entDecls, entRange)
            | None -> None
        | _ -> None

    let methodDecorators = Map.ofList ["TestFixtureSetUp", "before";
                                       "SetUp", "beforeEach";
                                       "Test", "it";
                                       "TearDown", "afterEach";
                                       "TestFixtureTearDown", "after"]

    let (|Test|_|) (decl: Fable.Declaration) =
        match decl with
        | Fable.MemberDeclaration(m,_,_,args,body,range) ->
            match m.Kind, (m.Decorators |> List.tryFind (fun x -> Map.containsKey x.Name methodDecorators)) with
            | Fable.Method, Some decorator -> Some (m, decorator, args, body, range)
            | _ -> None
        | _ -> None

    let [<Literal>] runSyncWarning = "Async.RunSynchronously must wrap the whole test"

    // Compile tests using Mocha.js BDD interface
    let transformTest (com: IBabelCompiler) ctx
                      (test: Fable.Member, decorator, args, body, range) =
        let buildAsyncTestBody range asyncBuilder =
            let doneFn = AST.Fable.Util.makeIdent "$done"
            let testBody =
                let doneFn = doneFn |> Fable.IdentValue |> Fable.Value
                let args = [asyncBuilder; doneFn; doneFn; doneFn]
                AST.Fable.Util.CoreLibCall("Async", Some "startWithContinuations", false, args)
                |> AST.Fable.Util.makeCall range Fable.Unit
            [doneFn], testBody
        match args with
        | [] -> ()
        | [arg: Fable.Ident] when arg.Type = Fable.Unit -> ()
        | _ -> failwithf "Test parameters are not supported (testName = '%s')." test.Name
        let testArgs, testBody =
            let (|RunSync|_|) = function
                | Fable.Sequential([Fable.Throw(Fable.Value(Fable.StringConst warning),_,_); arg],_)
                    when warning = runSyncWarning -> Some arg
                | _ -> None
            match body with
            | Fable.Apply(Fable.Value(Fable.Lambda(_,RunSync _, _)),[asyncBuilder],Fable.ApplyMeth,_,_)
            | RunSync asyncBuilder -> buildAsyncTestBody body.Range asyncBuilder
            | _ -> [], body
        let testBody =
            let args, body = com.TransformFunction ctx None testArgs testBody
            Babel.ArrowFunctionExpression (args, body, ?loc=testBody.Range) :> Babel.Expression
        let testName =
            Babel.StringLiteral test.Name :> Babel.Expression
        let testRange =
            match range, testBody.loc with
            | Some r1, Some r2 -> Some(r1 + r2) | _ -> None
        let newMethodName = methodDecorators.Item((decorator: Fable.Decorator).Name)
        // it('Test name', function() { /* Tests */ });
        Babel.ExpressionStatement(
            Babel.CallExpression(Babel.Identifier newMethodName,
                [U2.Case1 testName; U2.Case1 testBody], ?loc=testRange), ?loc=testRange)
        :> Babel.Statement

    let transformTestFixture (fixture: Fable.Entity) testRange testDecls =
        let testDesc =
            Babel.StringLiteral fixture.Name :> Babel.Expression
        let testBody =
            Babel.FunctionExpression([],
                Babel.BlockStatement (testDecls, ?loc=testRange), ?loc=testRange)
            :> Babel.Expression
        Babel.ExpressionStatement(
            Babel.CallExpression(Babel.Identifier "describe",
                [U2.Case1 testDesc; U2.Case1 testBody],
                ?loc=testRange)) :> Babel.Statement

    let asserts com (i: Fable.ApplyInfo) =
        match i.methodName with
        | "AreEqual" ->
            match i.args with
            | [expected; actual] -> Some [actual; expected]
            | [expected; actual; msg] -> Some [actual; expected; msg]
            | _ -> None
            |> Option.map (fun args ->
                Fable.Util.CoreLibCall ("Assert", Some "equal", false, args)
                |> Fable.Util.makeCall i.range i.returnType)
        | _ -> None

    let declareModMember range publicName privateName _isPublic isMutable _modIdent expr =
        let privateName = defaultArg privateName publicName
        Util.varDeclaration range (Util.identFromName privateName) isMutable expr
        :> Babel.Statement |> U2.Case1 |> List.singleton

    let castStatements (decls: U2<Babel.Statement, Babel.ModuleDeclaration> list) =
        decls |> List.map (function
            | U2.Case1 statement -> statement
            | U2.Case2 _ -> failwith "Unexpected export in test fixture")

open Util

type NUnitPlugin() =
    let helper =
        { new IDeclareMember with
            member __.DeclareMember(a,b,c,d,e,f,g) =
                declareModMember a b c d e f g }
    interface IDeclarePlugin with
        member x.TryDeclareRoot com ctx file =
            if file.Root.TryGetDecorator "TestFixture" |> Option.isNone then None else
            Util.transformModDecls com ctx helper None file.Declarations
            |> castStatements
            |> transformTestFixture file.Root (Some file.Range)
            |> U2.Case1
            |> List.singleton
            |> Some
        member x.TryDeclare com ctx decl =
            match decl with
            | Test (test, decorator, args, body, range) ->
                transformTest com ctx (test, decorator, args, body, range)
                |> List.singleton |> Some
            | TestFixture (fixture, testDecls, testRange) ->
                let ctx = { ctx with moduleFullName = fixture.FullName }
                Util.transformModDecls com ctx helper None testDecls
                |> castStatements
                |> transformTestFixture fixture testRange
                |> List.singleton |> Some
            | _ -> None

    interface IReplacePlugin with
        member x.TryReplace com info =
            match info.ownerFullName, info.methodName with
            | "NUnit.Framework.Assert", _ -> asserts com info
            | "Microsoft.FSharp.Control.FSharpAsync", "RunSynchronously" ->
                match info.returnType with
                | Fable.Unit ->
                    let warning = Fable.Throw(Fable.Value(Fable.StringConst Util.runSyncWarning), Fable.Unit, None)
                    AST.Fable.Util.makeSequential info.range [warning; info.args.Head] |> Some
                | _ -> failwithf "Async.RunSynchronously in tests is only allowed with Async<unit> %O" info.range
            | _ -> None
