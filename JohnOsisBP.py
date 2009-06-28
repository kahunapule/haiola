#! (tr1,tr2)
#! ndx should not generate text twice

# This file was originally developed by Nathan Miles of UBS, and modified by John Thomson of SIL International while on loan to The Seed Company.
# Used as part of Prophero by permission of the UBS.

#! had to change to test for type(None) in MarkerDefinitions
#! is there a conflict between subtype first and embedded for <verse>
#      when used for the first verse of a Psalm?

import re
import sys
import codecs

# Usage:
# OsisBP.initialize(scr, ShowScope)
# OsisBP.convertBooks(books, outFile)
#   where:
#       ScriptureText - text to be converted
#       ShowScope ("Yes" or "No") - include scope decorators in tags
#       books - string of ascii 0's and 1's indicating which book numbers to convert
#       outFile - name of file to contain result

#! generate chapter number earlier
#! comment
#! document globals

class ScopeDecorator:
    def __init__(self):
        self.prevPara = None
        self.thisPara = None
        self.prevID = ""

    # Called whenever ID line encountered.
    # Ensure that last verse in previous book is terminated.
    # Called with ID = None at end of all books.
    def newBook(self, ID):
        global ShowScope

        if ShowScope == "No": return        
        if ID == self.prevID: return
        
        if self.thisPara and self.prevID:
            vrs = XMLNode("verse", 90)
            vrs.setParm("eID", self.prevID)
            self.thisPara.children.append(vrs)
        
        self.prevPara = None
        self.thisPara = None
        self.prevOsisID = ""

    # Called on each new tag node.
    # Remeber the current and previous paragraphs containing
    # canonical text so we can correctly place previous verse
    # end tag if it needs to go at the end of the previous
    # paragraph.
    def newNode(self, node):
        global canonicalTags, ShowScope

        if ShowScope == "No": return        

        #! should we include "cell"
        if node.tagName in ["p", "q", "salute", "closer", "l", "item"]:
        #if canonicalTags.get(node.tagName, None):
            self.prevPara = self.thisPara
            self.thisPara = node

    # Called on each new verse.
    def newVerse(self, osisID):
        global stk, ShowScope

        if ShowScope == "No": return        

        # Update start and end attrributes for div's
        for node in stk:
            if node.tagName == "div" and node.parms.get("type", "") != "book":
                node.setParm("end", osisID)
                if not node.parms.get("start", None):
                    node.setParm("start", osisID)

        # If there was a previous verse        
        if self.prevID:
            node = stk[-1]
            vrs = XMLNode("verse", 90)
            vrs.setParm("eID", self.prevID)
            if len(node.children) or not self.prevPara:
                # Already some text in this paragraph,
                # add verse end in current paragraph.
                node.children.append(vrs)
            else:
                # Add verse end at end of previous paragraph
                self.prevPara.children.append(vrs)

        self.prevID = osisID


    # JohnT: Called to force immediate insert of end of previous verse
    def insertVerseEnd(self):
        node = stk[-1]
        vrs = XMLNode("verse", 90)
        vrs.setParm("eID", self.prevID)
        node.children.append(vrs)
        self.prevID = None       
        
# Escape embedded <, >, or &
def mapText(m):
    inp = m.group(0)
    if inp == "<": return "&lt;"
    if inp == ">": return "&gt;"
    return "&amp;"

# An XML tag and its children.
# Chlidren can be text data or other XMLNode's

class XMLNode:
    def __init__(self, tagName, level):
        self.tagName = tagName
        self.level = level
        self.children = []
        self.parms = {}
        scopeDecorator.newNode(self)
        
    def __repr__(self):
        return "<" + self.tagName + " " + str(self.level) + ">"

    # Set tag parameter value, e.g. "type"
    def setParm(self, name, value):
        self.parms[name] = value

    def appendText(self, text):
        if text:
            self.children.append(text)

    # Wrote representation of note to output
    def outputNode(self):
        # Make intro pseudo tag appear as <div>
        if self.tagName in ["intro", "outline"]:
            self.tagName = "div"

        start = self.parms.get("start", None)
        end = self.parms.get("end", None)
        if self.tagName == "div" and start and end:
            del self.parms["start"]
            del self.parms["end"]
            self.setParm("scope", start + "-" + end)
        
        parmText = ""
        for name in self.parms.keys():
            # Ed's insertion/mod:
            # Using the attribute path to output ide, rem, and restore 
            # as XML comments.
            if name in ["ide", "rem", "restore"]:
                parmText = " " + "\\" + name + " " + self.parms[name]
            else:
                parmText += " " + name + '="' + self.parms[name] + '"'
        
        if self.children:
            self.outputTag(self.tagName + parmText)
            for i in range(len(self.children)):
                child = self.children[i]
                if type(child) == type(stk[0]):
                    child.outputNode()
                else:
                    # If parent node is not a character style
                    if self.level < 90:
                        # If this is last child, strip any right space
                        if i == len(self.children)-1:
                            child = child.rstrip()
                        # If next to last child
                        elif i == len(self.children)-2:
                            next = self.children[-1]
                            if type(next) == type(stk[0]):
                                # Followed by a verse end marker
                                if next.parms.get("eID", ""):
                                    # Strip any right space
                                    child = child.rstrip()
                            
                    self.outputText(child)
                    
            self.outputTag("/" + self.tagName)
            
        # Ed's insertion/mod:
        # Close XML remark. We stored the actual remark text in parmText
        elif self.tagName == ("!--"):
            # JohnT: double-hyphens are illegal in XML comments, this is a bit crude but three passes will condense
            # any reasonable number.
            parmText = parmText.replace('--', '-')
            parmText = parmText.replace('--', '-')
            parmText = parmText.replace('--', '-')
            self.outputTag(self.tagName + parmText + "--")    
        else:
            self.outputTag(self.tagName + parmText + "/")

    # Write a begin or end tag.  Optionally preceed it with a \n depending on
    # its level.
    def outputTag(self, tagText):
        global output

        if tagText[0] == "/":
            if self.level < 50: output.write("\n")
        else:
            if self.level < 80: output.write("\n")
        output.write("<" + tagText + ">")

    # Output non-tag data
    def outputText(self, text):
        global replacements, output
        
        text = re.sub(r"<|>|&", mapText, text)  # escape special characters
        text = re.sub(r"[\r\n]", " ", text)     # make all whitespace spaces
        text = re.sub(r"\s\s+", " ", text)      # remove extra whitespace
            
        output.write(text)

    
# Info from the tagTable above is placed into objects of this class

class TagInfo:
    def __init__(self):
        self.tag = ""
        self.endTag = ""
        self.level = 0
        self.osisTag = ""
        self.osisType = ""
        self.environ = ""


# Mapping from SIL book ID to OSIS book name

idMap = {}
idMap['GEN'] = "Gen"
idMap['EXO'] = "Exod"
idMap['LEV'] = "Lev"
idMap['NUM'] = "Num"
idMap['DEU'] = "Deut"
idMap['JOS'] = "Josh"
idMap['JDG'] = "Judg"
idMap['RUT'] = "Ruth"
idMap['1SA'] = "1Sam"
idMap['2SA'] = "2Sam"
idMap['1KI'] = "1Kgs"
idMap['2KI'] = "2Kgs"
idMap['1CH'] = "1Chr"
idMap['2CH'] = "2Chr"
idMap['EZR'] = "Ezra"
idMap['NEH'] = "Neh"
idMap['EST'] = "Esth"
idMap['JOB'] = "Job"
idMap['PSA'] = "Ps"
idMap['PRO'] = "Prov"
idMap['ECC'] = "Eccl"
idMap['SNG'] = "Song"
idMap['ISA'] = "Isa"
idMap['JER'] = "Jer"
idMap['LAM'] = "Lam"
idMap['EZK'] = "Ezek"
idMap['DAN'] = "Dan"
idMap['HOS'] = "Hos"
idMap['JOL'] = "Joel"
idMap['AMO'] = "Amos"
idMap['OBA'] = "Obad"
idMap['JON'] = "Jonah"
idMap['MIC'] = "Mic"
idMap['NAM'] = "Nah"
idMap['HAB'] = "Hab"
idMap['ZEP'] = "Zeph"
idMap['HAG'] = "Hag"
idMap['ZEC'] = "Zech"
idMap['MAL'] = "Mal"
idMap['MAT'] = "Matt"
idMap['MRK'] = "Mark"
idMap['LUK'] = "Luke"
idMap['JHN'] = "John"
idMap['ACT'] = "Acts"
idMap['ROM'] = "Rom"
idMap['1CO'] = "1Cor"
idMap['2CO'] = "2Cor"
idMap['GAL'] = "Gal"
idMap['EPH'] = "Eph"
idMap['PHP'] = "Phil"
idMap['COL'] = "Col"
idMap['1TH'] = "1Thess"
idMap['2TH'] = "2Thess"
idMap['1TI'] = "1Tim"
idMap['2TI'] = "2Tim"
idMap['TIT'] = "Titus"
idMap['PHM'] = "Phlm"
idMap['HEB'] = "Heb"
idMap['JAS'] = "Jas"
idMap['1PE'] = "1Pet"
idMap['2PE'] = "2Pet"
idMap['1JN'] = "1John"
idMap['2JN'] = "2John"
idMap['3JN'] = "3John"
idMap['JUD'] = "Jude"
idMap['REV'] = "Rev"
idMap['TOB'] = "Tob"
idMap['JDT'] = "Jdt"
idMap['ESG'] = "AddEsth"
idMap['WIS'] = "Wis"
idMap['SIR'] = "Sir"
idMap['BAR'] = "Bar"
idMap['LJE'] = "EpJer"
idMap['S3Y'] = "PrAzar"
idMap['SUS'] = "Sus"
idMap['BEL'] = "Bel"
idMap['1MA'] = "1Macc"
idMap['2MA'] = "2Macc"
idMap['3MA'] = "3Macc"
idMap['4MA'] = "4Macc"
idMap['1ES'] = "1Esd"
idMap['2ES'] = "2Esd"
idMap['MAN'] = "PrMan"
idMap['PS2'] = "PssSol"

# Split a string into two parts at the first whitespace.
# If there is no second part, return "" for this.
# Always returns a list containing exactly two strings.

def splitFirst(text):
    if not text:
        return "",""
    
    parts = text.split(None,1)
    if len(parts) > 1:
        return parts[0], parts[1]
    return parts[0], ""

# Signal error back to Paratext list window

def printError(msg, arg=""):
    global id, chapter, verse
    
    if id == "???":
        print "# %s%s" % (msg.encode("utf-8"), arg.encode("utf-8"))
    else:
        print "%s %d:%s\t\t%s%s" % (id.encode("utf-8"), chapter, verse, msg, arg.encode("utf-8"))

def popStk(level = 0):
    global stk
    
    if level > 0:
        while stk[-1].level >= level:
            stk = stk[:-1]
    else:
        stk = stk[:-1]

def pushStk(osisTag, level, parms=None):
    global stk

    node = XMLNode(osisTag, level)
    if parms:
        node.parms = parms.copy()

    stk.append(node)
    if len(stk) > 1:
        stk[-2].children.append(node)
    
# Process the start of a new book by closing any open tags
# from a previous book and creating a new <div> for this book

def doId(text):
    global id, book, chapter, verse, chapterText
    
    chapter = 1
    chapterText = "1"
    verse = 0
    
    text = text.strip()
    id = text.split()[0].upper()
    book = idMap.get(id, "???")

    if book == "???":
        printError("Unknown book in \\id line: ", id)
        book = "???"

    # Finalize decoration for previous book and start new book
    scopeDecorator.newBook(book)

    while len(stk) > 2:
        popStk()

    pushStk("div", 10)    
    stk[-1].setParm("type", "book")
    stk[-1].setParm("osisID", book)

# Process the start of a new chapter by getting chapter number
# and resetting the verse number.

def doChapter(text, nextTag):
    global chapter, verse, chapterText, chapterLabel
    
    verse = 0

    text = text.strip()
    ch = text.split()[0]
    chapterText = ch
    
    try:
        chapter = int(ch)
    except:        
        printError("Invalid chapter number: ", ch)
        chapter = 0
    

    # Output a chapter milestone (after closing any open environments)
    if nextTag == "nb":
        #JohnT: chapter embedded in paragraph: don't close paragraph; close any previous verse
        scopeDecorator.insertVerseEnd()
        popStk(51)
    else:
         popStk(50)
    pushStk("chapter", 50)
    stk[-1].setParm("osisID", "%s.%s" % (book, chapterText))
    
    if chapterLabel:
        stk[-1].setParm("n", chapterLabel + " " + chapterText)
    else:
        stk[-1].setParm("n", chapterText)
        
    popStk()


# If \cl immediately follows \c then the text of the \cl marker becomes the
# name ("n" attribute) for this chapter.
def doChapterLabel(text):
    global chapterLabel
    
    text = text.strip()

    try:
        chapterNode = stk[-1].children[-1]
        tagName = chapterNode.tagName
    except:
        chapterNode = None
        
    if chapterNode and tagName == "chapter":
        chapterNode.setParm("type", "x-label")
        chapterNode.setParm("n", text)
    else:
        chapterLabel = text

# The \cp markers are used to indicate that the published text
# start a new chapter at this point.  This is used for non numeric
# chapter markers in EST/ESG. It also used for numeric chapter numbers
# when a chapter number must be repeated due verse(s) from a chapter
# being moved into the middle of another chapter.

def doChapterPublishable(text):
    global chapter, verse, chapterText
    
    verse = 0
    text = text.strip()
    chapterText = text.split()[0]


# Add to the osisId list a single entry

def formOsisIdSingle(osisId, book, chapterText, n):
    n = re.sub(r"(\d*)(\D+)", r"\1!\2", n)   # split out the verse subsegment if present
    id = book + "." + chapterText + "." + n
    osisId.append(id)

# Add to the osisId list the entries for a range of verse numbers

def formOsisIdRange(osisId, book, chapterText, nStart, nEnd):
    try:
        start = int(re.sub(r"\D+", "", nStart))
    except:
        printError("Invalid verse number: " + nStart)
        return
    
    try:
        end = int(re.sub(r"\D+", "", nEnd))
    except:
        printError("Invalid verse number: " + nEnd)
        return
    
    formOsisIdSingle(osisId, book, chapterText, nStart)
    
    start += 1
    while start < end:
        formOsisIdSingle(osisId, book, chapterText, str(start))
        start += 1
        
    formOsisIdSingle(osisId, book, chapterText, nEnd)

# Form the osisId list for a single \v entry. This may contain multiple entries
# separated by commas and/or ranges separated by dashes.

def formOsisId(book, chapterText, n):
    osisId = []
    parts = n.split(",")
    
    for part in parts:
        rangeParts = part.split("-");
        if len(rangeParts) > 1:
            formOsisIdRange(osisId, book, chapterText, rangeParts[0], rangeParts[1])
        else:
            formOsisIdSingle(osisId, book, chapterText, rangeParts[0])
    
    return " ".join(osisId)

# Process a new verse by getting the verse number and outputting
# a verse number milestone.

def doVerse(text, tt, i):
    global book, chapter, verse

    oldVerse = verse
    n, text = splitFirst(text)
    verse = n
    osisID = formOsisId(book, chapterText, n)
    scopeDecorator.newVerse(osisID)

    pushStk("verse", 50)
    
    #! This is a kludge
    # If the verse marker immediately follows another marker and that
    # marker is not an end marker, we assume this verse is embedded,
    # i.e. not the first verse its paragraph.
    if not (i-4 >= 0 and tt[i-3].strip() == "" and tt[i-4][-1] != "*"):
        stk[-1].setParm("subType", "x-embedded")
    
    stk[-1].setParm("osisID", osisID)
    stk[-1].setParm("n", n)
    
    if oldVerse == 0:
        stk[-1].setParm("subType", "x-first")
    if ShowScope == "Yes":
        stk[-1].setParm("sID", osisID)
    popStk()

    stk[-1].appendText(text)

# From:
# \fig |avnt016.jpg|span|||Some caption text|13.4\fig* 
#
# Make the OSIS converter generate: 
# <figure src="avnt016.jpg" type="x-span" alt="Some caption text" n="13.4"> 
# <caption>Some caption text</caption> 
# </figure> 

def doFig(text):
    parts = text.split("|")
    if len(parts) != 7:
        printError("Figure does not have exactly 7 fields. Ignored.", "")
        return
        
    pushStk("figure", 90)
    stk[-1].setParm("src", parts[1])

    if parts[2] == "col":
        stk[-1].setParm("type", "x-col")
    elif parts[2] == "span":
        stk[-1].setParm("type", "x-span")
    else:
        printError("Unknown figure type: ", parts[2])
        
    stk[-1].setParm("alt", parts[5])
    stk[-1].setParm("n", parts[6])
    
    pushStk("caption", 90)
    stk[-1].appendText(parts[5])

    popStk()
    popStk()


# Return true if this tagName is found in the stk

def inStk(tagName):
    global stk
    
    return len([x for x in stk if x.tagName == tagName])

# Some tags (e.g. \q, \tr, ...) will force certain environments
# to be opened, if not already open, to contain them.

def openEnviron(xmlNode):
    xmlNodeRole = xmlNode.parms.get("role", "")
    
    for node in stk:
        # A node must exist with correct tag, type, and level in order
        # to avoid creating a new node
        if node.tagName != xmlNode.tagName: continue
        if node.parms.get("type", "") != xmlNode.parms.get("type", ""): continue
        if node.level != xmlNode.level: continue
        
        # If the desired node has a required "role", e.g. \th1
        # and we have an entry in the stack with the required type
        # but an unspecified role, set the role in the stack to
        # be the required role.
        if xmlNodeRole:
            if not node.parms.get("role", ""):
                node.parms["role"] = xmlNodeRole
        
        return
            
    # Pop stack down to level of required node and then add it
    popStk(xmlNode.level)
    pushStk(xmlNode.tagName, xmlNode.level, xmlNode.parms)

# We don't need the whitespace right before a new paragraph marker.
# Remove it.

def removeParagraphFinalWhitespace(tt):
    i = 1
    while i<len(tt):
        tag = tt[i].rstrip()[1:]
        tagInfo = tags.get(tag, None)
        if (tagInfo and tagInfo.level == 50) or (tag == "c"):
            tt[i-1] = tt[i-1].rstrip()
        i = i + 2

# We get here when a \cat tag is found.
# What we should see is (\cat ...\cat*)+
# Gather all the categories; usually there is only one.
# Return a list of these separated by ,'s

def getCats(tt, i):
    cats = []
    tag1 =  i < len(tt) and tt[i] or ""
    
    while tag1.rstrip() == "\\cat":
        text1 =  i+1 < len(tt) and tt[i+1] or ""
        tag2 =  i+2 < len(tt) and tt[i+2] or ""
        text2 =  i+3 < len(tt) and tt[i+3] or ""

        cats.append(text1)        
        
        if tag2 == "\\cat*":
            i = i+4
        else:
            printError(r"\cat without \cat*", "")
            i = i+2
            
        tag1 =  i < len(tt) and tt[i] or ""
        if tag1.rstrip() == "\\cat":
            pass
            
    return i, ",".join(cats), text2

# Extract the n (caller) parameter and the subType parameter
# from a note. Advance the current index (i) past the
# subType information if present.  If the caller information
# is present at the beginning of the text without a following
# marker, strip it off the text.
# Return (text, parms, i)

def noteParms(text, parms, tt, i):
    # If this is a note tag, make the first token be the value
    # of the "n" parameter.
    tag1 =  i < len(tt) and tt[i] or ""

    # case: \env 1-2 \cat Unclear\cat* blah blah\env*
    # <note type="x-verse" subType="Unclear" n="1-2">
    if tag1.rstrip() == "\\cat":
        i, categories, text2 = getCats(tt, i)
        parms["subType"] = categories
        parms["n"] = text.rstrip()
        text = text2
                    
    # case: \enk sacrifice \cat1 blah blah\enk*
    # <note type="x-keyword" subType="1" n="sacrifice">
    elif tag1.startswith("\\cat"):
        parms["subType"] =  tag1[4:].rstrip()
        parms["n"] = text.rstrip()
        text = i+1 < len(tt) and tt[i+1] or ""

        i = i+2

    # case: \f + blah blah\f*
    # <note type="x-f" n="+">
    else:
        if text:
            caller, text = splitFirst(text)
            parms["n"] = caller

    return (text, parms, i)   

def doSection(tagInfo, text):
    # If the last child of the currently open tag was a <chapter>
    # we move this tag to be the first node of the new section.
    chapterChild = None
    try:
        node = stk[-1]
        lastChild = node.children[-1]
        if lastChild.tagName == "chapter":
            chapterChild = lastChild
            node.children = node.children[:-1]
    except:
        pass
            
    # Remove anything of higher or equal level to the desired section
    reqdLevel = tagInfo.osisTags[-2].level
    while  reqdLevel <= stk[-1].level:
        popStk()
        
    # Open the div(s)
    for i in range(0, len(tagInfo.osisTags)-1):
        openEnviron(tagInfo.osisTags[i])
    
    # If we found a <chapter> previously, add it here
    if chapterChild:
        stk[-1].children.append(chapterChild)
        
    pushStk("title", 60)
    stk[-1].appendText(text)


# Ed's insertion/mod
# The description element is only valid as a child of work element which is
# a grandchild of osisText which is fixed as stk[1].
#def doDescription(tagInfo, level, parms, text):
    #desc = XMLNode(tagInfo, level)
    #if parms:
        #desc.parms = parms.copy()
    #desc.appendText(text)
    #stk[1].children[0].children[0].children.append(desc)

# In order to correctly treat items in list,
# if a list item is already opened it must be closed.

def popListItem(tagInfo):
    if "item" in [x.tagName for x in stk]:
        while stk[-1].tagName != "item":
            popStk()
        popStk()
                        
    
# Convert the text of a chapter

def convertText(text):
    global tags, endTags
    
    # Split text into markers and data
    tt = re.split(r"(\\[a-zA-Z0-9]+\*|\\[a-zA-Z0-9]+\s*)", text)

    removeParagraphFinalWhitespace(tt)    

    i = 1
    while i<len(tt):
        # Get tag (w/o backslash) and tag text
        tag = tt[i].rstrip()[1:]
        text = tt[i+1]
        i = i + 2
        
        # Make old style rows into new style rows
        if tag in ["tr1", "tr2"]:
            tag = "tr"

        # xt*, xt*, fig* are just a return to normal text when we are already in normal
        # text, so we ignore this except for appending any following text
        if tag in ["ft*", "xt*", "fig*"]:
            stk[-1].appendText(text)
            continue

        # If this is an end tag, close all tags down to and including
        # the matching opening tag.
        tagInfo = endTags.get(tag, None)
        if tagInfo:
            if inStk(tagInfo.osisTag):
                while stk[-1].tagName != tagInfo.osisTag:
                    popStk()
                popStk()
            else:
                printError("Unmatched end tag ignored: ", tag)
            stk[-1].appendText(text)
            continue

        # Handle \id
        if tag == "id":
            doId(text)
            continue
        
        # Handle \v
        if tag == "v":
            doVerse(text, tt, i)
            continue

        # Handle \c
        if tag == "c":
            nextTag = tt[i].rstrip()[1:]
            doChapter(text, nextTag)
            continue

        # Handle \cl
        if tag == "cl":
            doChapterLabel(text)
            continue

        # Handle \cp
        if tag == "cp":
            doChapterPublishable(text)
            # "continue" omitted intentionally, we still need to generate the tag

        # Handle \fig        
        if tag == "fig":
            doFig(text)
            continue
        
        # Handle \b.
        # The valid way to use a \b is to end a poetic line group (stanza).
        # If it is used some other way, just generate a <lb> which will
        # in most cases be ignored by the typesetting software.
        if tag == "b":
            if inStk("lg"):
                popStk(50)
            else:
                popStk(50)
                pushStk("lb", 50)
                popStk(50)
            continue
        
        # \ib is like \b but in an intro
        if tag == "ib":
            if inStk("lg"):
                popStk(60)
            else:
                popStk(60)
                pushStk("lb", 60)
                popStk(60)
            continue
        
        ## Handle \m
        #if tag == "m":
            #if inStk("p"):
                #popStk(51)
                #stk[-1].appendText(text)
            #else:
                #popStk(50)
                #parms = { "type" : "x-m" }
                #pushStk("p", 50, parms)
                #stk[-1].appendText(text)
            #continue

        # JohnT: reinstate this; we are handling \nb.
        # Ignore \nb since it indicates that there was no paragraph
        # break present (just a marker indicating that the paragraph
        # spanned a chapter boundary.
        if tag == "nb":
            stk[-1].appendText(text)
            continue

        # \ft and \xt are generic character style close markers.
        # Close all character styles.
        if tag == "ft" or tag == "xt":
            if stk[-1].level == 90:
                popStk()
            stk[-1].appendText(text)
            continue

        tagInfo = tags.get(tag, None)
        if not tagInfo:
            printError("Unknown tag and its text ignored: ", "\\" + tag)
            continue

        if tag in ["s", "s1", "s2", "s3", "ms", "ms1", "ms2", "is", "is1", "is2"]:
            doSection(tagInfo, text)
            continue

        # Lists with items are a bit tricky. See popListItem for details.
        if "item" in [x.tagName for x in tagInfo.osisTags]:
            popListItem(tagInfo)
    
        # Close all higher level tags. 
        reqdLevel = tagInfo.osisTags[-1].level
        while  stk[-1].level >= reqdLevel:
            popStk()

        # Open any environments needed by this tag 
        for j in range(len(tagInfo.osisTags)-1):
            openEnviron(tagInfo.osisTags[j])
    
        parms = tagInfo.osisTags[-1].parms.copy()
        
        # We use the dummy value --- to indicate that the text of the marker
        # is to be used as a value of the attribute, instead of as the text
        # for this tag.
        for name in parms.keys():
            val = parms[name]
            if val == "---":
                parms[name] = text.strip()
                text = ""
        
        # If this is a note tag, get values of n and subType attribute
        if tagInfo.level == 80:
            text, parms, i = noteParms(text, parms, tt, i)
            
        # Ed's insertion/mod:  
        # Handle ide rem and restore. 
        # JK disabled - if (tag in ["ide", "rem", "restore"]):
            # "description" element is only valid as child of "work" element. 
            # test to see if these tags form a single block after id so USFM file 
            # can be reconstructed.
            # NLM -- I'm not so sure that all the stuff really fits a common
            #        idea of "description".  I think for now we will force
            #        all these to be a comment.
            # EA -- Per OSIS best practice, these are description elements.
            #       However, their context precludes mapping them to "description".
            #test = 1
            #for n in range (i-4, 0, -2):
            #    if tt[n].rstrip()[1:] not in ["id", "ide", "rem", "restore"]: 
            #        test = 0
            #if test == 1:
            #    doDescription(tagInfo.osisTag, tagInfo.level, parms, text)
            #    continue
            
            # Output as comment JK disabled, EA re-enabled
        if (tag in ["ide", "rem", "restore"]):
            tagInfo.osisTag = "!--"
            parms.clear()
            parms[tag] = text.strip()
            text = ""

        # Push this tag and its level onto the stacks
        pushStk(tagInfo.osisTag, tagInfo.level)
        stk[-1].parms = parms

        #??????????????????????
        # Some character tags are natural units.
        # Don't include their trailing whitespace.
        if tag in ['xo', 'fr']:
            text = text.rstrip()

        stk[-1].appendText(text)


def convertToTags(levels, xpath):
    if xpath.startswith('"'): xpath = xpath[1:]
    if xpath.endswith('"'): xpath = xpath[:-1]
    
    xpath = xpath.replace('""', '"')
    xpath = xpath.replace('//', '/')
    xpath = xpath.replace(" and ", " ")
    
    levels = levels.replace('"', '')
    levels = levels.replace(" ", "")
    tags = []
    
    parts = xpath.split("/")
    levels = levels.split(",")
    if len(parts) != len(levels):
        printError("xmltag.txt: levels/tags mismatch: " + str(levels) + " | " + str(xpath))
        return tags
    
    for i in range(len(parts)):
        part = parts[i].strip()
        part = part.replace("'", '"')
        
        try:
            if "[" in part:
                tag, part = part.split("[",1)
            else:
                tag = part
                part = ""

            node = XMLNode(tag, int(levels[i]))
            for piece in re.findall(r'\w+="[^"]+"', part):
                name, val = piece.split("=")
                
                node.parms[name.strip()] = val[1:-1]
            tags.append(node)
        except:
            printError("xmltag.txt: invalid tags syntax: " + xpath)
    
    return tags

# Return the directory name where this module is loaded from.
# Sadly this cannot be moved to a separate module since __file__ refers to
# module containing this source.  A copy of this is found in
# CreateInDesignDocument.py, OsisBP.py, and PACreateUXDocument.py.
# All three must be updated when this is changed.

def moduleBaseDirectory():
    global __name__
    if __name__=="__main__":
        moduleFile = sys.argv[0]
    else:
        moduleFile = __file__
    parts = moduleFile.split("\\")
        
    return "\\".join(parts[:-1])


# Read info about how to convert each USFM tag

def readTagTable():
    global tags, endTags, canonicalTags
    tags = {}
    endTags = {}
    canonicalTags = {}
    failed = 0
    
    # If I use just open for some reason it goes to the wrong directoy
    # so we calculate a path to the module directory
    if moduleBaseDirectory():
        f = open(moduleBaseDirectory() + "\\xmltag.txt")
    else:
        f = open("xmltag.txt")
    
    for line in f:
        line = line.strip()
        parts = line.split("\t")
        if not parts[0]: continue
        if parts[0].endswith("***"): continue

        parts[0] = parts[0].strip()
        tagInfo = TagInfo()
        tagInfo.tag = parts[0]
        
        tagInfo.osisTags = convertToTags(parts[1], parts[2])
        if tagInfo.osisTags:
            tagInfo.osisTag = tagInfo.osisTags[-1].tagName
            tagInfo.level = tagInfo.osisTags[-1].level
        else:
            failed = 1
            
        if tagInfo.level == 80 or tagInfo.level >= 90:
            tagInfo.endTag = tagInfo.tag + "*"
            endTags[tagInfo.endTag] = tagInfo

        tags[tagInfo.tag] = tagInfo
        
    #!!! do something with failed
    f.close()

# Convert selected books into named output file

# myChanges - Must be either None or a routine which takes a single
# text string as an argument and returns a transformed string.

def convertBooks(infile, outfile, codecIn):
    global id, book, chapter, chapterText, verse, chapterLabel
    global levelStk, tagStk, output, scr, stk, scopeDecorator
    
    file = codecs.open(infile, "r", codecIn)
    text = file.read()
    file.close()
    
    scopeDecorator = ScopeDecorator()
    readTagTable()
    
    id = "???"
    book = "???"
    chapter = 1
    chapterText = ""
    verse = 0
    chapterLabel = ""
   

    stk = []

    writer = codecs.lookup("utf-8")[3]
    output = writer(open(outfile, "w"))
    
    # EA - following line added for xml validity 
    output.write('<?xml version="1.0" encoding="UTF-8" standalone="yes"?>')

    print "# OSIS XML output written to: " + outfile

    # Output file header information
    pushStk("osis", 0)
    stk[-1].setParm("xmlns", "http://www.bibletechnologies.net/2003/OSIS/namespace")
    stk[-1].setParm("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance")
    # former
    #stk[-1].setParm("xsi:schemaLocation", "http://www.bibletechnologies.net/osisCore.2.0.xsd")
    
    # Added by EA - NOTE: relative URL for schemaLocation which is in "My Paratext Projects". XML chokes on spaces in URL's
    stk[-1].setParm("xsi:schemaLocation", "http://www.bibletechnologies.net/2003/OSIS/namespace file:../osisCore.2.0_UBS_SIL_BestPractice.xsd")

    pushStk("osisText", 1)
    stk[-1].setParm("osisIDWork", "thisWork")
    stk[-1].setParm("xml:lang", "x-" + Language)

    pushStk("header", 2)
    pushStk("work", 3)
    stk[-1].setParm("osisWork", "thisWork")
    popStk()
    popStk()
            
    convertText(text)

    scopeDecorator.newBook(None)
    
    stk[0].outputNode()
    stk = []

    output.close()    

def reCompile(pattern):
    try:
        return re.compile(pattern)
    except:
        return None    

def undoEscape(m):
    return unichr(int(m.group(0)[2:],16))
        
def replaceEscapedChars(s):
    s = s + u""
    return re.sub(r"\\u....", undoEscape, s)

   
if __name__=="__main__":
    global ShowScope, id, Language
    
    id = "???"
    ShowScope = "Yes"    
    Language = "Kup"
    convertBooks(sys.argv[1], sys.argv[2], sys.argv[3])

