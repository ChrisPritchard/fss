let main () =
    let mutable i = 0 in
    let mutable acc = 0 in
    (while i < 10 do
       (if i % 2 = 0 then acc <- acc + i else ();
        i <- i + 1);
     print_int acc; print_newline ())
