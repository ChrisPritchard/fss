type P = { X: int }

let main () =
    print_bool ([{ X = 1 }; { X = 2 }] = [{ X = 1 }; { X = 2 }]); print_newline ()
