let rec rev acc xs = match xs with [] -> acc | h :: t -> rev (h :: acc) t
let main () =
    print_int (match rev [] [1; 2; 3; 4] with a :: b :: c :: d :: [] -> a * 1000 + b * 100 + c * 10 + d | _ -> 0);
    print_newline ()
