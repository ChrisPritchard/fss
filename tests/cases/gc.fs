let rec length xs = match xs with | [] -> 0 | h :: t -> 1 + length t

let main () =
    let mutable big = [] in
    let mutable i = 0 in
    (while i < 50000 do (big <- i :: big; i <- i + 1);
     let mutable j = 0 in
     let mutable n = 0 in
     (while j < 50000 do
        (let mutable k = 0 in
         let mutable xs = [] in
         (while k < 100 do (xs <- k :: xs; k <- k + 1); n <- n + 1);
         j <- j + 1);
      print_int (length big); print_newline ();
      print_int n; print_newline ()))
