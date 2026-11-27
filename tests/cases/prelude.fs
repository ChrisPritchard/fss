// The fss prelude (List / Option / String). Also valid F#: same output.
type Person = { Name: string; Age: int }

let asInt b = if b then 1 else 0

[<EntryPoint>]
let main _ =
    let people =
        [ { Name = "ann"; Age = 30 }
          { Name = "bob"; Age = 25 }
          { Name = "cy"; Age = 41 } ]

    System.Console.WriteLine(List.length people)
    System.Console.WriteLine(asInt (List.isEmpty people))
    System.Console.WriteLine(String.concat ", " (List.map (fun p -> p.Name) people))

    let adults =
        people |> List.filter (fun p -> p.Age >= 30) |> List.map (fun p -> p.Age)
    System.Console.WriteLine(List.fold (fun acc -> fun a -> acc + a) 0 adults)

    let names = List.map (fun p -> p.Name) people
    System.Console.WriteLine(asInt (List.contains "bob" names))
    System.Console.WriteLine(asInt (List.exists (fun p -> p.Age > 40) people))
    System.Console.WriteLine(asInt (List.forall (fun p -> p.Age > 18) people))
    System.Console.WriteLine(List.length (List.rev people))
    System.Console.WriteLine(List.length (List.distinct [1; 2; 1; 3]))
    System.Console.WriteLine(List.length (List.zip [1; 2] [3; 4]))
    System.Console.WriteLine(List.item 1 (List.map (fun p -> p.Age) people))
    System.Console.WriteLine(List.length (List.take 2 people))
    System.Console.WriteLine(List.length (List.skip 1 people))

    match List.tryHead people with
    | Some p -> System.Console.WriteLine(p.Name)
    | None -> System.Console.WriteLine("none")

    let oldest =
        List.fold (fun best -> fun p -> if p.Age > best then p.Age else best) 0 people
    let oldestAge =
        List.tryFind (fun p -> p.Age = oldest) people |> Option.map (fun p -> p.Age)
    System.Console.WriteLine(Option.defaultValue 0 oldestAge)

    System.Console.WriteLine(String.length "hello")
    System.Console.WriteLine(asInt (Option.isSome (Option.map (fun x -> x + 1) (Some 1))))
    let nothing = Option.defaultValue 7 None
    System.Console.WriteLine(nothing)
    0
