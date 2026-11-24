let main () =
    let mutable i = 0 in
    let mutable acc = 0 in
    (while i <= 10 do (acc <- acc + i; i <- i + 1);
     print_int acc; print_newline ())
