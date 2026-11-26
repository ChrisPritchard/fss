// cstring (char*) interop: NUL-terminated C strings in extern signatures.
#include <string.h>
#include <ctype.h>

extern strlen(s: cstring) : int
extern strcmp(a: cstring, b: cstring) : int
extern strchr(s: cstring, c: int) : cstring
extern toupper(c: int) : int

let main () =
    let s = cstring_of_string "hello"
    println (strlen s)
    println (cstring_len s)
    println (string_of_cstring s)
    println (strcmp (cstring_of_string "a") (cstring_of_string "a"))
    println (s = cstring_of_string "hello")
    println (s = cstring_of_string "world")
    println (cstring_is_null s)
    println (cstring_is_null (cstring_null ()))
    let hit = strchr s 108
    println (string_of_cstring hit)
    println (cstring_is_null (strchr s 122))
    println (string_of_cstring (cstring_of_string ("ab" + "cd")))
    println (toupper 97)
    0
