let main () =
    let mutable i = 0
    let mutable total = 0
    while i < 10 do
        total <- total + i
        i <- i + 1
    print_int total
    print_newline ()
