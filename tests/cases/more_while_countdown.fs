let mutable i = 5
let mutable r = 1
let main () =
    while i > 0 do (r <- r * i; i <- i - 1);
    print_int r;
    print_newline ()
