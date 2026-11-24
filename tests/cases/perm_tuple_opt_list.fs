let t = (Some [1; 2], (3, "x"))
let main () =
    print_int (match t with (o, (n, s)) -> (match o with Some l -> (match l with a :: b :: [] -> a + b | _ -> 0) | None -> 0) + n);
    print_newline ()
