let rec map f xs = match xs with [] -> [] | h :: t -> f h :: map f t
let main () =
    print_int (match map (fun x -> x + 1) [1; 2] with a :: b :: [] -> a + b | _ -> 0);
    print_newline ();
    print_string (match map (fun x -> x + "!") ["a"; "b"] with a :: b :: [] -> a + b | _ -> "?");
    print_newline ()
