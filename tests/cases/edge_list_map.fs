let rec map f xs = match xs with [] -> [] | h :: t -> f h :: map f t
let main () =
    print_int (match map (fun x -> x + 1) [1; 2; 3] with a :: b :: c :: [] -> a + b + c | _ -> 0);
    print_newline ()
