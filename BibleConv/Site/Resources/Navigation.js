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