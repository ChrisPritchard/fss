let rec filter p xs = match xs with [] -> [] | h :: t -> if p h then h :: filter p t else filter p t
let main () =
    print_int (match filter (fun x -> x % 2 = 0) [1; 2; 3; 4; 5; 6] with a :: b :: c :: [] -> a + b + c | _ -> 0);
    print_newline ()
