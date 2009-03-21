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

function TocPath(path)
{
	var indexH = path.lastIndexOf("-");
	var indexDot = path.lastIndexOf(".");
	if (indexH != -1)
	{
		 return path.substring(0, indexH + 1) + "TOC" + path.substring(indexDot);
	}
	return path;
}