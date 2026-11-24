let x = ((1, 2), (3, 4))
let main () =
    print_int (match x with ((a, b), (c, d)) -> a + b + c + d);
    print_newline ()
