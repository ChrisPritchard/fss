[<EntryPoint>]
let main _ =
    let mutable acc = ""
    let mutable i = 0
    while i < 20000 do
        acc <- acc + "x"
        i <- i + 1
    System.Console.WriteLine(acc)
    0
