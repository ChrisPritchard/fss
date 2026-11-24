let t = ([1; 2], "x", Some 3)
let main () =
    print_int (match t with (xs, s, o) -> (match xs with a :: b :: [] -> a + b | _ -> 0) + (match o with Some n -> n | None -> 0));
    print_newline ()
