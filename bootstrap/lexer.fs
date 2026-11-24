// A tokenizer for fss source, written in fss itself.
// Reads a file path from argv and prints one token per line.

let isSpace c = c = 32 || c = 9 || c = 10 || c = 13
let isDigit c = 48 <= c && c <= 57
let isUpper c = 65 <= c && c <= 90
let isLower c = 97 <= c && c <= 122
let isAlpha c = isUpper c || isLower c || c = 95

let rec skip s i n inComment =
    if i >= n then i
    elif inComment then
        (let c = str_get s i in
         if c = 10 then skip s (i + 1) n false
         else skip s (i + 1) n true)
    else
        (let c = str_get s i in
         if isSpace c then skip s (i + 1) n false
         elif c = 47 && i + 1 < n && str_get s (i + 1) = 47 then skip s (i + 2) n true
         else i)

let rec scanIdent s i n =
    if i >= n then i
    else (let c = str_get s i in
          if isAlpha c || isDigit c then scanIdent s (i + 1) n else i)

let rec scanNum s i n =
    if i >= n then i
    else (let c = str_get s i in
          if isDigit c then scanNum s (i + 1) n else i)

let rec scanStr s i n =
    if i >= n then i
    else (let c = str_get s i in
          if c = 34 then i + 1 else scanStr s (i + 1) n)

let rec lex s i n =
    if i >= n then ()
    else
        (let j = skip s i n false in
         if j >= n then ()
         else
            (let c = str_get s j in
             if isAlpha c then
                 (let k = scanIdent s (j + 1) n in
                  (print_string ("ident:" + str_sub s j (k - j)); print_newline ();
                   lex s k n))
             elif isDigit c then
                 (let k = scanNum s (j + 1) n in
                  (print_string ("int:" + str_sub s j (k - j)); print_newline ();
                   lex s k n))
             elif c = 34 then
                 (let k = scanStr s (j + 1) n in
                  (print_string ("str:" + str_sub s (j + 1) (k - j - 2)); print_newline ();
                   lex s k n))
             else
                 (print_string ("sym:" + str_sub s j 1); print_newline ();
                  lex s (j + 1) n)))

let main argv =
    match argv with
    | _ :: f :: rest ->
        let s = read_file f in
        lex s 0 (str_len s)
    | _ -> (print_string "usage: lexer <file>"; print_newline ())
