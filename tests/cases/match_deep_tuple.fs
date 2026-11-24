let main () =
    let v = ((1, (2, 3)), Some 4) in
    print_int (match v with | ((a, (b, c)), Some d) -> a + b + c + d | _ -> 0); print_newline ()
