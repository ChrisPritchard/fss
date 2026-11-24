let rec rev acc xs = match xs with [] -> acc | h :: t -> rev (h :: acc) t
let main () =
    print_int (match rev [] [1; 2] with a :: b :: [] -> a + b | _ -> 0);
    print_newline ();
    print_string (match rev [] ["a"; "b"] with a :: b :: [] -> a + b | _ -> "?");
    print_newline ()
