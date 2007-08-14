// JavaScript functions to highlight text in main window of concordance.

function onLoad()
{
	if (parent.conc && parent.conc.curWord)
		selectWord(parent.conc.curWord, parent.conc.curFlags);
}
function selectWord(text, flags) {
	if (parent.main.curWord == text)
		return false;
	var para = document.getElementsByTagName("body")[0];
	var pattern = new RegExp("\\b" + text +"\\b", flags);
	selectWordIn(para, text, pattern);
	parent.main.curWord = text;
}
function selectWordIn(para, text, pattern)
{
	var len = text.length;
	if (para.childNodes == null)
		return -1; // some weird component we can't process.
	for (var ichild = 0; ichild < para.childNodes.length; ichild++)
	{
		var mytext = para.childNodes[ichild];
		if (mytext.nodeType == 1 && mytext.getAttribute("class") == "concHighlight")
		{
			// we found something we previously inserted!
			var first = mytext.firstChild;
			if (first == null)
			{
				// This is a bizarre thing that only seems to happen in IE. Somehow the replaceChild
				// seems to leave an empty span with no parents in the children of this.
				// If this happens, ignore the node.
				continue;
			}
			var contents = first.data;
			if (contents != null && contents.length == len && contents.search(pattern) == 0)
				continue; // leave correct span alone
			// algorithm has been previously applied with a different target; clean up.
			var replacement = document.createTextNode(contents);
			para.replaceChild(replacement, mytext);
			para.normalize(); // merge adjacent text nodes
			ichild = -1; // start over.
			continue;
		}
		var before = para.childNodes[ichild + 1]; // null for last, child to insert pieces before.
		var input = mytext.data;
		if (input == null)
		{
			if (mytext.nodeType == 1)
				selectWordIn(mytext, text, pattern); // recursively process children.
			continue; // not the right sort of child
		}
		var offset = input.search(pattern);
		if (offset < 0)
			continue;
		mytext.data = input.substring(0, offset);
		appendSpan(input, offset, len, para, before); ichild++;
		do
		{
			input = input.substring(offset + len);
			offset = input.search(pattern);
			if (offset < 0)
			{
				appendText(input, 0, input.length, para, before); ichild++; // add the tail end
				break;
			}
			appendText(input, 0, offset, para, before); ichild++; // append text between occurrences
			appendSpan(input, offset, len, para, before); ichild++ // append another occurrence.		
		} while (true)
	}
}

function appendText(input, offset, len, parent, before)
{
	if (len == 0)
		return -1; // don't make empty spans.
	var textNode = document.createTextNode(input.substring(offset, offset + len))
	if (before == null)
		parent.appendChild(textNode);
	else
	parent.insertBefore(textNode, before);
}

function appendSpan(input, offset, len, parent, before)
{
	if (len == 0)
		return -1; // don't make empty spans.
	var span = document.createElement("span");
	var textNode = document.createTextNode(input.substring(offset, offset + len))
	span.appendChild(textNode);
	span.style.background = "rgb(0,255,255)";
	if (before == null)
		parent.appendChild(span);
	else
		parent.insertBefore(span, before);
	span.setAttribute("class", "concHighlight");
}
