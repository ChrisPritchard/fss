let main () =
    print_int (match [1; 2; 3] with a :: b :: c :: [] -> a + b + c | _ -> 0);
    print_newline ()
