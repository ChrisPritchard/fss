let main () =
    let id = fun x -> x in
    (print_int (id 1); print_bool (id true))
