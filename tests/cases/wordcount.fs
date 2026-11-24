let isSpace c = c = 32 || c = 9 || c = 10 || c = 13

let rec skipSpaces s i n =
    if i >= n then i
    elif isSpace (str_get s i) then skipSpaces s (i + 1) n
    else i

let rec wordEnd s i n =
    if i >= n then i
    elif isSpace (str_get s i) then i
    else wordEnd s (i + 1) n

let rec countWords s i n acc =
    let j = skipSpaces s i n in
    if j >= n then acc
    else (let k = wordEnd s j n in countWords s k n (acc + 1))

let main () =
    let s = read_file "tests/cases/wordcount.txt" in
    (print_int (countWords s 0 (str_len s) 0); print_newline ())
