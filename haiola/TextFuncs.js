// JavaScript functions to highlight text in main window of concordance.

var hilitedElt = "";
var oldBackground;

function onLoad()
{
    hilitedElt = "";
    var temp = getUrlVar("w");
    if (temp && temp != "")
        selectWord(temp, getUrlVar("f"));
    RemoveUnwantedNavButton();
}

function RemoveUnwantedNavButton() {
    if (this != top)
        RemoveNavButton("showNav");
    else
        RemoveNavButton("hideNav")
}

function RemoveNavButton(id) {
    var button = document.getElementById(id);
    if (button)
        button.parentNode.removeChild(button);
}
// Given a query string "?to=email&why=because&first=John&Last=smith"
// getUrlVar("to") will return "email"
// getUrlVar("last") will return "smith"

// Slightly more concise and improved version based on http://www.jquery4u.com/snippets/url-parameters-jquery/
function getUrlVar(key) {
    var result = new RegExp(key + "=([^&]*)", "i").exec(window.location.search);
    return result && unescape(result[1]) || "";
}

function onLoadBook(bookName)
{
	onLoad();
	if (parent && parent.parent && parent.parent.navigation)
		parent.parent.navigation.SetBookName(bookName);
	if (parent && parent.parent && parent.parent.parent && parent.parent.parent.navigation)
		parent.parent.parent.navigation.SetBookName(bookName);
}
function selectWord(text, flags) {
	var para = document.getElementsByTagName("body")[0];
	var pattern = new RegExp("(^|\\W)" + text +"($|\\W)", flags);
	selectWordIn(para, text, pattern);
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
		if (offset == 0)
		{
			// We need to figure out whether we matched ^ or \W
			var temp = input.substring(0, len);
			if (temp.search(pattern) < 0)
				offset++; // must have matched on initial \W
		}
		else
		{
			// We must have matched \W
			offset++; // to start of actual word.
		}
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
			offset++; // this time we MUST be matching initial \W
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

function hilite(eltId)
{
	if (hilitedElt != "")
	{
		var prev = document.getElementById(hilitedElt);
		if (prev)
		{
			prev.style.background = oldBackground;
		}
		hilitedElt = "";
	}
	var target = document.getElementById(eltId);
	if (target)
	{
		hilitedElt = eltId;
		oldBackground = target.style.background;
		target.style.background = "rgb(128,255,255)";
	}
}