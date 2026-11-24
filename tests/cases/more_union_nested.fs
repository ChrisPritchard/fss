type A = B of int | C of A
let rec depth a = match a with B _ -> 0 | C a2 -> 1 + depth a2
let main () =
    print_int (depth (C (C (B 1))));
    print_newline ()
