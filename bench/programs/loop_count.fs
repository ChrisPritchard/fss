[<EntryPoint>]
let main _ =
    let mutable x = 0
    let mutable i = 0
    while i < 100000000 do
        x <- x + 1
        i <- i + 1
    printf "%d\n" x
    0
