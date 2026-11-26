let collatz_len n =
    let mutable steps = 0
    let mutable m = n
    while m <> 1 do
        if m % 2 = 0 then m <- m / 2 else m <- 3 * m + 1
        steps <- steps + 1
    steps

[<EntryPoint>]
let main _ =
    let mutable total = 0
    let mutable i = 1
    while i <= 100000 do
        total <- total + collatz_len i
        i <- i + 1
    System.Console.WriteLine(total)
    0
