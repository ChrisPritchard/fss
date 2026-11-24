let main () =
    let t = ((1, 2), (3, 4)) in
    print_int (match t with | ((a, b), (c, d)) -> a + b + c + d); print_newline ()
