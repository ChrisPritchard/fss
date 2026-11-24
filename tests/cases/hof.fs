let choose b = if b then (fun x -> x + 1) else (fun x -> x - 1)

let main () =
    print_int ((choose true) 41); print_newline ();
    print_int ((choose false) 41); print_newline ()
