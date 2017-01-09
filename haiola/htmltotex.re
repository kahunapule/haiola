|<a href="([^"]*)"[^>]*>([^☻]*?)</a>|\\href\{$1\}\{$2\}|
|<a href='([^']*)'[^>]*>([^☻]*?)</a>|\\href\{$1\}\{$2\}|
|<a> *([^☻]*?)</a>|\\href\{$1\}\{$1\}|
|<a>([^☻]*?)</a>|\\href\{$1\}\{$1\}|
|<img[^>]*>|\[\]| Strip out pictures, for now.
|<h1[^>]*>([^☻]*?)</h1>|\{\\TINYIMT $1\\par\}|
|<h2[^>]*>([^☻]*?)</h2>|\{\\TINYIMTB $1\\par\}|
|<h3[^>]*>([^☻]*?)</h3>|\{\\TINYIMTC $1\\par\}|
|<h4[^>]*>([^☻]*?)</h3>|\{\\TINYIMTD $1\\par\}|
|<p[^>]*>([^☻]*?)</p>|\{\\TINY $1\\par\}|
|<p[^>]*>|\\par\}\{\\TINY |
|<a>||
|</a>||
|<br */>|\\vskip 1ex |
|<br>|\\vskip 1ex |
|<ul>||
|</ul>||
|<li>([^☻]*?)</li>|\{\\TINYLI $1\\par\}|
|<b>|\\BDB |
|</b>|\\BDE |
|<table[^>]*>| |\\begin\{center\}\\begin\{tabular\}\{ l l \}|
|</table>| |\\end\{tabular\}\\end\{center\}|
|<tr[^>]*>| |[^☻]*?<td[^>]*>([^☻]*?)</td[^>]*>[^☻]*?<td[^>]*>([^☻]*?)</td[^>]*>[^☻]*?</tr[^>]*>|$1 & $2 \\\\|
|<td[^>]*>| |
|</td>| |
|</tr>| |
|<hr\s*/>|\\hrule |
|<hr>|\\hrule |
|<><>No promoVersionInfo<><>||
