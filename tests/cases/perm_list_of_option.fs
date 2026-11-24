let xs = [Some 1; None; Some 3]
let main () =
    print_int (match xs with a :: b :: c :: [] -> (match a with Some n -> n | None -> 0) + (match b with Some n -> n | None -> 0) + (match c with Some n -> n | None -> 0) | _ -> 0);
    print_newline ()
