let xs = [[1; 2]; [3]; []]
let main () =
    print_int (match xs with a :: b :: c :: [] -> (match a with x :: y :: [] -> x + y | _ -> 0) + (match b with x :: [] -> x | _ -> 0) | _ -> 0);
    print_newline ()
