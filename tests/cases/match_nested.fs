let main () =
    let v = Some (1 :: 2 :: []) in
    print_int (match v with | None -> 0 | Some (h :: t) -> h | Some [] -> 0); print_newline ()
