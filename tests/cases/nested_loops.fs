let main () =
    let mutable i = 0 in
    let mutable count = 0 in
    (while i < 3 do
       (let mutable j = 0 in
        (while j < 4 do (count <- count + 1; j <- j + 1);
         i <- i + 1));
     print_int count; print_newline ())
