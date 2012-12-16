/\r(\n)/$1/ #collapse Windows newlines to just \n
/\|fn([^\|\n]*?)\n?\\f ([^\|]*?)\\f\*/\f $2$n\f* $1/ material between |fn and following \f moves after subsequent \f*. (part of old move_footnote_to_fn.cct)

/\n(\\(x|\*x|f|\*f))/$1/ // remove newlines before these four marker groups (old cleanup_OW_to_USFM.cct)

/\n/$r$n/ # may eventually remove this to an optional table that restores Windows-style newlines. For now it must be the last replacment