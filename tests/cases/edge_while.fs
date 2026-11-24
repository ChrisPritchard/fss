let mutable i = 0
let mutable acc = 0
let main () =
    while i < 5 do (acc <- acc + i; i <- i + 1);
    print_int acc;
    print_newline ()
