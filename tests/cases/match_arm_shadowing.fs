let main () =
    let x = 100 in
    (print_int (match (Some 1) with | Some x -> x + 1 | None -> 0); print_newline ();
     print_int x; print_newline ())
