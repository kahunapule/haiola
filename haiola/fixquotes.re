//—/
//—/
//‘/
//’/
//“/
//”/
/<<</“‘/
/>>>/’”/
/<</“/
/>>/”/
/</‘/
/>/’/
/' "/'"/
/" '/"'/
/“ ‘/“‘/
/’ ”/’”/
/(\s)"/$1“/
/(\s)"/$1“/
/"(\s)/”$1/
/(\w\S*)"/$1”/
/"(\S*\w)/“$1/
/=[“”"]([^“”"]*)[“”"]/="$1"/
/---/—/
/--/—/
/\\restore [^\\]*//
/\\rem [^\\]*//
/\\v (\d*)\,(\d*) /\v $1-$2 / Fix bad verse bridge syntax
/\\v (\d*)\–(\d*) /\v $1-$2 / Fix n-dash where dash belongs
/\\v (\d*)\—(\d*) /\v $1-$2 / Fix m-dash where dash belongs
/// Strip out ^Z characters (CPM EOF leftovers)
/// Remove invisible garbage.
/\u0085/ / Remove next line (NEL) characters.

