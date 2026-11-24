let main () =
    let v = (Some 1, [2; 3]) in
    print_int (match v with | (Some x, h :: t) -> x + h | (None, _) -> 0 | (_, []) -> 0); print_newline ()
