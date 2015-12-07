namespace DisinheritTypeProvider

open System.Reflection
open System.Text
open FSharp.Quotations
open ProviderImplementation.ProvidedTypes
open FSharp.Core.CompilerServices

[<TypeProvider>]
type DisinheritedProvider (config:TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces ()

    let toParams (m:MethodBase) =
        let ps = m.GetParameters() 
        [for p in ps -> ProvidedParameter(p.Name, p.ParameterType, p.IsOut)]

    let addMember (def:ProvidedTypeDefinition) ty (mem:MemberInfo) =
        match mem.MemberType with
        | MemberTypes.Constructor ->
            let ci = mem :?> ConstructorInfo
            let c = ProvidedConstructor(toParams ci,InvokeCode=fun args -> Expr.Coerce(Expr.NewObject(ci,args), typeof<obj>))
            def.AddMember(c)
        | MemberTypes.Field ->
            let fi = mem :?> FieldInfo
            let field = ProvidedField(mem.Name, fi.FieldType)
            def.AddMember(field)
        | MemberTypes.Property ->
            let pi = mem :?> PropertyInfo
            let prop = ProvidedProperty(mem.Name, pi.PropertyType) 
            prop.GetterCode <- fun args -> Expr.PropertyGet(Expr.Coerce(args.[0],ty), pi)
            def.AddMember(prop)
        | MemberTypes.Event ->
            let ei = mem :?> EventInfo
            let ev = ProvidedEvent(mem.Name, ei.EventHandlerType) 
            ev.AdderCode <- fun args -> Expr.Call(Expr.Coerce(args.Head,ty),ei.GetAddMethod(), args.Tail)
            ev.RemoverCode <- fun args -> Expr.Call(Expr.Coerce(args.Head,ty), ei.GetRemoveMethod(), args.Tail)
            def.AddMember(ev)
        | MemberTypes.Method ->
            let mi = mem :?> MethodInfo
            if not mi.IsSpecialName then
                let m = ProvidedMethod(mi.Name, toParams mi, mi.ReturnType)
                m.InvokeCode <- fun args -> Expr.Call(Expr.Coerce(args.Head,ty), mi, args.Tail)
                def.AddMember(m)
        | _ -> ()

    let rec createXmlDoc (ty:System.Type) =
        let s = System.Text.StringBuilder("<summary>")
        s.AppendLine(ty.FullName) |> ignore
        let mutable t = ty.BaseType
        let mutable i = 1
        while t <> null do
            s.Append("<para>&lt;") |> ignore
            s.Append('-', i) |> ignore
            s.Append(t.FullName) |> ignore
            s.AppendLine("</para>") |> ignore
            t <- t.BaseType
            i <- i + 1
        s.Append("</summary>") |> ignore
        s.ToString()

    let rec heirarchy (ty:System.Type) =
        match ty with
        | null -> []
        | ty -> ty::heirarchy(ty.BaseType)

    let providedTypes (types:System.Type[]) level =
        [for ty in types |> Seq.where(fun x -> x.IsPublic) ->
            let def = ProvidedTypeDefinition(ty.Name, baseType=Some typeof<obj>, HideObjectMethods=true)
            def.AddXmlDocDelayed(fun () -> createXmlDoc ty)
            let instance = ProvidedProperty("__Instance", ty)
            instance.GetterCode <- fun args -> Expr.Coerce(args.[0],ty)
            def.AddMember(instance)
            let heirarchy = heirarchy ty
            let members = 
                ty.GetMembers() 
                |> Array.filter (fun m ->
                    match heirarchy |> List.tryFindIndex (fun t -> t = m.DeclaringType) with
                    | Some i -> i <= level
                    | None -> false
                )
            for mem in members do addMember def ty mem
            let from = ProvidedMethod("From", [ProvidedParameter("instance",ty)], def)
            from.IsStaticMethod <- true
            from.InvokeCode <- fun args -> Expr.Coerce(args.[0],def)
            def.AddMember(from)
            def
        ]

    let ns = "Disinherit"
    let asm = System.Reflection.Assembly.GetExecutingAssembly()
    let providedType = ProvidedTypeDefinition(asm, ns, "Disinherited", Some typeof<obj>)
    do  providedType.DefineStaticParameters(
            staticParameters=[ProvidedStaticParameter("assemblyName", typeof<string>);
                              ProvidedStaticParameter("level", typeof<int>, parameterDefaultValue=0)],
            apply=(fun typeName parameterValues ->
                let name = 
                    match parameterValues.[0] with
                    | :? string as assembly -> assembly
                    | _ -> invalidArg "assemblyName" "Expecting assembly name"
                let level =
                    match parameterValues.[1] with
                    | :? int as level -> level
                    | _ -> invalidArg "level" "Expecting level"
                let ty = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>)
                let types = Assembly.Load(name).GetTypes()
                ty.AddMembersDelayed(fun () -> providedTypes types level)
                ty
            )
        )
    do  this.AddNamespace(ns, [providedType])
