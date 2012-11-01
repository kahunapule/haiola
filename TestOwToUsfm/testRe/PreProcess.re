/\r(\n)/$1/ #collapse Windows newlines to just \n
/ *(\n|$)/$1/  # remove spaces at end of line. They can end up between end of sentence and note marker.
/(?<=(\n|^))\\(bt|ov|nt|al|e|chk2|bnvt|nq)(.|\n)*?(\n(?=\\)|$)// # removes various (possibly multi-line) fields. 