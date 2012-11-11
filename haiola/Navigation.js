// JavaScript functions to highlight text in main window of concordance.

function SetBookName(bookName)
{
	var target = document.getElementById("book");
	var replacement = document.createTextNode(bookName);
	for (var i = 0; i < target.childNodes.length; i++)
	{
		if (target.childNodes[i].nodeType == 3)
		{
			target.replaceChild(replacement, target.childNodes[i])
			return 0;
		}
	}
	// no suitable child nodes to replace.
	target.appendChild(replacement);
}

function gotoStartOfBook() {
    if (parent.body.main) {
        pathToBook = parent.location.pathname.substring(0, parent.location.pathname.length - 6); // strip off NN.htm
        chapNum = "00";
        lastChar = pathToBook[pathToBook.length - 1];
        if (lastChar == '0' || lastChar == '1') // Psalms
        {
            pathToBook = pathToBook.substring(0, pathToBook.length - 1);
            chapNum = "000";
        }
        parent.location.assign(parent.location.protocol + parent.location.host + pathToBook + chapNum + ".htm");
    }
    // something different if in concordance pane: todo johnt -- get working
    else parent.body.concholder.main.location = parent.body.concholder.main.location.pathname;
}