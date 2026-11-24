let mutable i = 0
let mutable sum = 0
let main () =
    while i < 10 do (sum <- sum + i; i <- i + 1);
    print_int sum;
    print_newline ()
