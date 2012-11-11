/\r(\n)/$1/ #collapse Windows newlines to just \n
/(\n)\n+/$1/ # remove multiple newlines.
/\n(?!\\)/ / # newlines not before markers change to spaces (but note \vt special case)
/^((.|\n)*)(\\id .*(\n|$))/$3$1/ # anything before the first \id moves after it. Note we explicitly include newlines in the match.

/>>>/’”/ # special case: three closing quotes map to single then double
/<</“/
/>>/”/
/</‘/
/>/’/

/\n\\(b|m|m2|p|pi|q|q2|q3|qc|qm) /$n\$1$n/ #these markers require following newline, not space. Rule must come after nl->space rule.
/\\(_|e|z).*\n// # material following these markers is removed.
/\\(cat|ref|cap|des).*\n// # material related to figures is removed for now.
/\\rcrd /\rem RCRD: /
/\\s( |\n)(([^\\]|\n)*)\[\\s2\]/\s2 $2/ # \s marked with [\s2] at end becomes \s2
/\\s( |\n)/\s1 /

/\\ft( |\n)(.[^\\]*?): ([^\\]*)/\f + \fr $2 \ft $3$n\f*$n/ # this group before we remove \vt which may be needed to terminate. Re-arranges some parts of footnotes.
/(?<=\n)\\ft( |\n)(.[^\\]*)/\f + \fr $2$n\f*$n/ # previous newline needed to prevent reconverting \ft added by previous pattern

/\\vt( |\n)/$n/ #this intentionally leaves a newline behind that is not followed by a marker.
/\\st( |\n)/\mt2 /
/\\hist /\rem HIST:  / # no idea why we want two spaces, following old table.

/(\n\\(bt|al|ov|t|nt|ntgk|ntck|chk2|dt|dtb|ud|chk) .*)\[\\s2\]/$1[s2]/ # convert internal markers only relevant inside notes. Do this after converting isolated newlines.
/(\n\\(bt|al|ov|t|nt|ntgk|ntck|chk2|dt|dtb|ud|chk) .*)\[\\q3\]/$1[q3]/
/(\n\\(bt|al|ov|t|nt|ntgk|ntck|chk2|dt|dtb|ud|chk) .*)\\va /$1variant /
/(\n\\(bt|al|ov|t|nt|ntgk|ntck|chk2|dt|dtb|ud|chk) .*)\\ant /$1antonym /
/(\n\\(bt|al|ov|t|nt|ntgk|ntck|chk2|dt|dtb|ud|chk) .*)\\mr\)/$1morphology)/
/(\n\\(bt|al|ov|t|nt|ntgk|ntck|chk2|dt|dtb|ud|chk) .*)\\mr /$1morphology/ # don't know why we would kill the space, but following John Duerkson's model...maybe never tested this case?
/(\n\\(bt|al|ov|t|nt|ntgk|ntck|chk2|dt|dtb|ud|chk) .*)\\lf /$1lexical function /
/(\n\\(bt|al|ov|t|nt|ntgk|ntck|chk2|dt|dtb|ud|chk) .*)\\ov\)/$1older version)/
/(\n\\(bt|al|ov|t|nt|ntgk|ntck|chk2|dt|dtb|ud|chk) .*)\\ov\]/$1older version]/

/(\n\\(bt|al|ov|t|nt|ntgk|ntck|chk2|dt|dtb|ud|chk)[^\n\\]*)/$1\note*$n/ # terminate all notes (before we change their initial markers)
/\n\\bt( |\\note*)/$n\note BT:$1/
/\n\\al( |\\note*)/$n\note Alternate:$1/
/\n\\ov( |\\note*)/$n\note OV:$1/
/\n\\t( |\\note*)/$n\note T:$1/
/\n\\nt( |\\note*)/$n\note NT: $1/ # for some mysterious reason the rest of these notes insert an extra space. Just following the original...
/\n\\ntgk( |\\note*)/$n\note NTGK: $1/
/\n\\ntck( |\\note*)/$n\note NTCK: $1/
/\n\\dt( |\\note*)/$n\note DT: $1/
/\n\\dtb( |\\note*)/$n\note DTB: $1/
/\n\\ud( |\\note*)/$n\note UD: $1/
/\n\\chk( |\\note*)/$n\note CHK: $1/
/\n\\chk2( |\\note*)/$n\note CHK2: $1/

/\\qh /\li$n/

/\\cf( |\n)(.[^\\]*?):(( |\n)[^\\]*)/\x + \xo $2 \xt$3$n\x*$n/
/\\cf( |\n)(.[^\\]*)/\x + \xo $2$n\x*$n/

/([^\n])\\(?!(note|xo|xt|fr|f))/$1***\/ # mark non-start-of-line markers as problems, with a few exceptions

/\|b([^\n|]*)\|i([^\n|]*)\|r/\bd $1\it $2\it* \bd* /
/\|i([^\n|]*)\|b([^\n|]*)\|r/\it $1\bd $2\bd* \it* / # not in original CC table; will terminate \it* \bd *
/\|b([^\n|]*)\|r/\bd $1\bd* /
/\|i([^\n|]*)\|r/\it $1\it* /

/(\n)\n+/$1/ # remove multiple newlines (again, in case we introduced some).

/\n/$r$n/ # eventually remove this to an optional table that restores Windows-style newlines. For now it must be the last replacment