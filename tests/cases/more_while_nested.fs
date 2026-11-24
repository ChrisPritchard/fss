let mutable i = 0
let mutable c = 0
let main () =
    while i < 3 do (let mutable j = 0 in (while j < 3 do (c <- c + 1; j <- j + 1); i <- i + 1));
    print_int c;
    print_newline ()
