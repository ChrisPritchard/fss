let main () =
    let a = 10 in
    let f = fun x -> (fun y -> x + y + a) in
    print_int (f 1 2); print_newline ()
