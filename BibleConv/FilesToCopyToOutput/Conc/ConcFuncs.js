// JavaScript functions used in the concordance pane.

function sel(text, flags) {
	if (parent.main.selectWord)
		parent.main.selectWord(text, flags);
}
