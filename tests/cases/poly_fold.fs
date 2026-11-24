let rec fold f acc xs = match xs with | [] -> acc | h :: t -> fold f (f acc h) t

let main () =
    (print_int (fold (fun a -> fun b -> a + b) 0 [1; 2; 3; 4]); print_newline ();
     print_bool (fold (fun a -> fun b -> a && b) true [true; true; false]); print_newline ())
