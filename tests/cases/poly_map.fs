let rec map f xs = match xs with | [] -> [] | h :: t -> f h :: map f t

let main () =
    (print_bool (map (fun x -> x * 2) [1; 2; 3] = [2; 4; 6]); print_newline ();
     print_bool (map (fun b -> not b) [true; false] = [false; true]); print_newline ())
