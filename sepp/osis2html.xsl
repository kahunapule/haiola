<xsl:stylesheet version="1.0"
                xmlns:osis="http://www.bibletechnologies.net/2003/OSIS/namespace"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
				xmlns:msxsl="urn:schemas-microsoft-com:xslt"
				xmlns:user="urn:nowhere">
	
<xsl:output method="html" encoding="UTF-8" doctype-public="-//W3C//DTD XHTML 1.0 Transitional//EN" doctype-system="http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"/>


<!-- XSLT stylesheet for Bible text in OSIS format (output to HTML, with display.css file) -->
<!-- Dave van Grootheest, Netherlands Bible Society, modified by John Thomson, SIL -->
<!-- last updated: 2009-05-31 -->



<!--
<xsl:param name="startId"/>
<xsl:param name="endId"/>
-->
	<xsl:param name="copyright"/>

<xsl:variable name="bookid" select="//osis:div[@type='book']/@osisID"/>
<xsl:variable name="bookname" select="//osis:div[@type='book']//osis:title[text()][1]"/>
<xsl:variable name="source" select="/osis:osis/osis:osisText/osis:header//osis:source"/>
<xsl:variable name="textversion0" select="concat(2, substring-before(substring-after($source, '(2'), ')'))"/>
<xsl:variable name="textversion" select="concat(substring($textversion0, 9), '-', substring($textversion0, 6, 2), '-', substring($textversion0, 1, 4))"/>



<xsl:template match="/">
	<html>
    <head>
      <link rel="stylesheet" href="display.css" type="text/css"/>
		<xsl:comment><![CDATA[[if IE]>
			<link href="display-ie.css" rel="stylesheet" type="text/css" />
			<![endif]]]></xsl:comment>
		<xsl:comment><![CDATA[[if lte IE 6]>
			<link href="display-ie6.css" rel="stylesheet" type="text/css" />
			<![endif]]]></xsl:comment>
		<script src="TextFuncs.js" type="text/javascript"></script>
		<script type="text/javascript">var curBook="<xsl:value-of select="//osis:title[@level='1']"/>"</script>
	</head>
	<!--xsl:variable name="book" select="//osis:title[@level='1']"/-->
	<body onload="onLoadBook(curBook)" class="mainDoc">

      <xsl:apply-templates select="//osis:div[@type='book' or @type='majorSection']"/>
		<div class="footnotes">
			<xsl:if test="//osis:note[not(@type='crossReference')]">
				<hr/>
			</xsl:if>
			<xsl:apply-templates select="//osis:note[not(@type='crossReference')]" mode="endnotes"/>
			<xsl:if test="//osis:note[@type='crossReference']">
				<hr/>
			</xsl:if>
			<xsl:apply-templates select="//osis:note[@type='crossReference']" mode="endnotes"/>
		</div>
		<!--JohnT: IE seems to cut off the last line; insert a blank.-->
		<div>
			<br/>
		</div>

	</body>
  </html>

</xsl:template>

<xsl:template match="osis:hi[@type='emphasis']" mode="endnotes">

  <i><xsl:apply-templates/></i>

</xsl:template>
	
<xsl:template match="osis:hi[@type='italic']" mode="endnotes">

  <i><xsl:apply-templates/></i>

</xsl:template>
	
<xsl:template match="osis:hi[@type='emphasis']" mode="tooltip">

  <i><xsl:apply-templates/></i>

</xsl:template>
	
<xsl:template match="osis:hi[@type='small-caps']" mode="endnotes">
	<span class="smallcaps"><xsl:apply-templates/></span>
</xsl:template>
	
<xsl:template match="osis:hi[@type='italic']" mode="tooltip">

  <i><xsl:apply-templates/></i>

</xsl:template>

<xsl:template match="osis:osis">

  <xsl:apply-templates select="//osis:osisText"/>

</xsl:template>



<xsl:template match="osis:osisText">

  <xsl:apply-templates select="osis:div"/>

</xsl:template>



<xsl:template match="osis:div[@type='book' or @type='majorSection']">

	<!-- If there are sections, use them to make a table of contents.-->
  <xsl:if test="osis:div[@type='section']">

    <xsl:choose>
      <xsl:when test="osis:title/osis:title[@level='2']">

		  <!-- JohnT: was this; but Chuck uses multiple level 2 titles, sometimes before level 1, e.g.
		The letter of Paul/to the church at/Ephesus
      <div class="maintitle1"><xsl:value-of select="osis:title/osis:title[@level='1']"/></div>
      <div class="maintitle2"><xsl:value-of select="osis:title/osis:title[@level='2']"/></div>
		-->
		  <xsl:apply-templates select="osis:title/osis:title" mode="title"/>
		  <div class="copyright">
			  <xsl:value-of select="$copyright" />
		  </div>
	  </xsl:when>
      <xsl:otherwise>

        <div class="maintitle">
          <xsl:apply-templates select="osis:title[@type='main']/osis:title">
            <xsl:with-param name="skip-note">yes</xsl:with-param>
          </xsl:apply-templates>
        </div>
      </xsl:otherwise>
    </xsl:choose>


    <table cellpadding="2" align="center" style="font-size: smaller">

    <xsl:for-each select="osis:div[@type='section']">
    
      <xsl:variable name="firstchapterid" select="substring-before(substring-after(descendant::osis:verse[@sID][1]/@osisID, '.'), '.')"/>
      <xsl:variable name="firstverseid" select="descendant::osis:verse[@sID][1]/@n"/>

      <xsl:variable name="lastchapterid" select="substring-before(substring-after(descendant::osis:verse[@sID][last()]/@osisID, '.'), '.')"/>
      <xsl:variable name="lastverseid" select="descendant::osis:verse[@sID][last()]/@n"/>

      <xsl:variable name="range">
        <xsl:value-of select="$firstchapterid"/>
        <xsl:text>:</xsl:text>
        <xsl:choose>
          <xsl:when test="contains($firstverseid, '-')">
            <xsl:value-of select="substring-before($firstverseid, '-')"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="$firstverseid"/>
          </xsl:otherwise>
        </xsl:choose>
          
        <xsl:text> - </xsl:text>

        <xsl:value-of select="$lastchapterid"/>
        <xsl:text>:</xsl:text>
        <xsl:choose>
          <xsl:when test="contains($lastverseid, '-')">
            <xsl:value-of select="substring-after($lastverseid, '-')"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="$lastverseid"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:variable>


      <tr>
        <td align="right">
          <xsl:number count="osis:div[@type='section']"/>
          <xsl:text>.</xsl:text>
        </td>
        <td>
          <xsl:choose>
            <xsl:when test="osis:title">
              <a href="#{position()}">
                <xsl:apply-templates select="osis:title[not(@type='parallel')]"/>
              </a>
            </xsl:when>
            <xsl:otherwise>
              <xsl:text>[</xsl:text>
              <xsl:value-of select="$range"/>
              <xsl:text>]</xsl:text>
            </xsl:otherwise>
          </xsl:choose>
        </td>
        <td>
          <xsl:if test="$range != ': - :'">
            <xsl:text>(</xsl:text>
            <xsl:value-of select="$range"/>
            <xsl:text>)</xsl:text>
          </xsl:if>
        </td>
      </tr>
    
    </xsl:for-each>

    </table><xsl:text>&#10;</xsl:text>
    <br/><xsl:text>&#10;</xsl:text>

    <hr/><xsl:text>&#10;</xsl:text>

  </xsl:if>

  <br/><xsl:text>&#10;</xsl:text>

	<!-- Find the most promising element and make a main heading.-->
  <xsl:choose>
    <xsl:when test="osis:title/osis:title[@level='2']">

		<!-- JohnT: was this; but Chuck uses multiple level 2 titles, sometimes before level 1, e.g.
		The letter of Paul/to the church at/Ephesus
      <div class="maintitle1"><xsl:value-of select="osis:title/osis:title[@level='1']"/></div>
      <div class="maintitle2"><xsl:value-of select="osis:title/osis:title[@level='2']"/></div>
		-->
		<xsl:apply-templates select="osis:title/osis:title" mode="title"/>
    </xsl:when>
    <xsl:otherwise>

      <div class="maintitle">
        <xsl:apply-templates select="osis:title[@type='main']/osis:title">
          <xsl:with-param name="skip-note">yes</xsl:with-param>
        </xsl:apply-templates>
      </div>

    </xsl:otherwise>
  </xsl:choose>

	<!-- JohnT: in the original this was present and the applyTempaltes had no select. But then if the 'if' succeeds all the main sections get included twice!
	  This if appears to be to insert anything that precedes the main sections. I'm not sure how to get whatever might be matched by
	  the text()[.!='&#10;']. Currently I don't want any introductory material in the main document...I generate any introduction separately.-->
	<!--xsl:if test="osis:p | osis:list | osis:lg | osis:lb | osis:milestone | osis:speaker | osis:q | osis:div[@type='x-highLevelPoetryDivision'] | text()[.!='&#10;']">
    <div class="text">
      <xsl:apply-templates select="osis:p | osis:list | osis:lg | osis:lb | osis:milestone | osis:speaker | osis:q | osis:div[@type='x-highLevelPoetryDivision']"/>
    </div>
  </xsl:if-->

  <!-- <xsl:apply-templates select="osis:div[@type='section' and not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])]"/> -->
  <xsl:apply-templates select="osis:div[@type='section'] | osis:chapter"/>

</xsl:template>



<xsl:template match="osis:title[@type='main']/osis:title">

  <xsl:param name="skip-note"/>


  <xsl:choose>
    <xsl:when test="$skip-note='yes'">
      <xsl:value-of select="."/>
    </xsl:when>
    <xsl:otherwise>
      <xsl:apply-templates/>
    </xsl:otherwise>
  </xsl:choose>

</xsl:template>
	
	<!-- JohnT rules for main titles.-->
	<xsl:template match="osis:title[@level=1]" mode="title">
		<div class="maintitle1">
			<xsl:value-of select="."/>
		</div>
	</xsl:template>
	<xsl:template match="osis:title[@level=2]" mode="title">
		<div class="maintitle2">
			<xsl:value-of select="."/>
		</div>
	</xsl:template>



	<xsl:template match="osis:div[@type='section']">

  <!-- <xsl:if test="osis:title and starts-with(@scope, concat($startId, '-')) or osis:title and preceding::osis:verse[@sID = $startId]"> -->
  <xsl:if test="osis:title">
    <div class="sectionheading"><a name="{position()}"/><xsl:apply-templates select="osis:title[not(@type='parallel')]"/></div>
  </xsl:if>

  <div class="text">
    <!-- <xsl:apply-templates select="osis:p[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:list[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:lg[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:milestone[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:lb[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])]"/> -->
    <xsl:apply-templates select="osis:p | osis:list | osis:lg | osis:lb | osis:milestone | osis:chapter | osis:speaker | osis:q | osis:title[@type='parallel'] | osis:div[@type='x-highLevelPoetryDivision'] | text()[.!='&#10;']"/>
  </div>
	<!-- JohnT: This allows handling subsections embedded in divisions.-->
	<xsl:apply-templates select="osis:div"/>

</xsl:template>
  
  <xsl:template match="osis:title[@type='parallel']">
    <div class="parallel">
      <xsl:value-of select="."/>
    </div>
  </xsl:template>

<xsl:template match="osis:div[@type='subSection']">

	<!-- <xsl:if test="osis:title and starts-with(@scope, concat($startId, '-')) or osis:title and preceding::osis:verse[@sID = $startId]"> -->
	<xsl:if test="osis:title">
    <xsl:choose>
    <xsl:when test="parent::osis:div[@type='subSection']">
      <div class="sectionsubsubheading">
        <!--JohnT: this doesn't generate exactly what I'd like but at least they're unique. position() on its own
		    repeats in each section.-->
        <a name="{count(preceding::osis:div[@type='section'])}.{count(preceding::osis:div[@type='subSection'])}.{position()}">
          <xsl:value-of select="osis:title"/>
        </a>
      </div>
    </xsl:when>
    <xsl:otherwise>
	    <div class="sectionsubheading">
		    <!--JohnT: this doesn't generate exactly what I'd like but at least they're unique. position() on its own
		    repeats in each section.-->
		    <a name="{count(preceding::osis:div[@type='section'])}.{position()}">
			    <xsl:value-of select="osis:title"/>
		    </a>
	    </div>
    </xsl:otherwise>
    </xsl:choose>
	</xsl:if>
	<xsl:if test="osis:title[@type='parallel']">
		<div class="parallelSub">
			<xsl:value-of select="osis:title[@type='parallel']"/>
		</div>
	</xsl:if>

	<div class="text">
		<!-- <xsl:apply-templates select="osis:p[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:list[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:lg[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:milestone[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:lb[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])]"/> -->
		<xsl:apply-templates select="osis:p | osis:list | osis:lg | osis:lb | osis:chapter | osis:milestone | osis:q | osis:speaker | osis:q | osis:div[@type='x-highLevelPoetryDivision'] | osis:div[@type='subSection'] |text()[.!='&#10;']"/>
	</div>
	<!-- JohnT: if we have sub-sub-sections do something here.-->
</xsl:template>

<xsl:template match="osis:p">

  <xsl:choose>
    <xsl:when test="@type='x-block'">

      <div class="proseblock">
        <xsl:apply-templates/>
      </div>

    </xsl:when>
    <xsl:when test="@type='x-blockIndent'">

      <div class="proseblockindent">
        <xsl:apply-templates/>
      </div>

    </xsl:when>
    <!--
    <xsl:when test="*[1]/following::osis:verse[@sID=$startId] or substring-after(preceding-sibling::*[local-name(.) = 'chapter'][1]/@osisID, '.') = '1' or local-name(preceding-sibling::*[local-name(.) != 'chapter'][1]) = 'title' or local-name(preceding-sibling::*[local-name(.) != 'chapter'][1]) = 'milestone' and preceding-sibling::*[local-name(.) != 'chapter'][1]/@type='x-blankLine'">

      <div class="proseblock">
        <xsl:apply-templates/>
      </div>

    </xsl:when>
    -->
    <xsl:otherwise>

      <div class="prose">
        <xsl:apply-templates/>
      </div>

    </xsl:otherwise>
  </xsl:choose>

</xsl:template>

	<!--JohnT: embedded quote. Following the pattern of osis:lg, if there are two adjacent ones
	insert a break.-->
	<xsl:template match="osis:q">
		<xsl:if test="name(preceding-sibling::*[1])='q'">
			<br/>
		</xsl:if>
		
		<xsl:apply-templates/>
	</xsl:template>

<xsl:template match="osis:lg">

  <xsl:if test="name(preceding-sibling::*[1])='lg'">
    <br/>
  </xsl:if>

  <xsl:apply-templates/>

</xsl:template>



<xsl:template match="osis:l">

	<xsl:choose>
    <xsl:when test="@level>2">
      <div class="deepPoetry">
        <xsl:apply-templates/>
      </div>
    </xsl:when>
    <xsl:when test="@level=2">
			<div class="embeddedPoetry">
				<xsl:apply-templates/>
			</div>

		</xsl:when>
		<xsl:otherwise>
			<div class="poetry">
				<xsl:apply-templates/>
			</div>
		</xsl:otherwise>
	</xsl:choose>
	<xsl:apply-templates select="osis:note[@type='x-quoteSource']" mode="quoteSource"/>
		
</xsl:template>



<xsl:template match="osis:list">

  <xsl:apply-templates/>

</xsl:template>



<xsl:template match="osis:item">

  <div class="list">
    <xsl:apply-templates/>
  </div>

</xsl:template>

	<msxsl:script language="JScript" implements-prefix="user">
		<![CDATA[
		function anchors(firstVerse, lastVerse, chapter) {
			result = "";
			var start = Number(firstVerse); // alphabeticaly "9" > "10"
			var finish = Number(lastVerse);
      if (start == 1)
        start = 2;
			for (i = start; i <= finish; i++)
				result = result + "<a name=\"C" + chapter + "V" + i + "\"/>";
			return result;		}
		]]>
	</msxsl:script>

  <!-- JohnT: insert chapter anchor and CnV1 anchor-->
  <xsl:template match="osis:chapter">
    <xsl:variable name="chapter" select="@n"/>
    <a name="C{$chapter}"/><a name="C{$chapter}V1"/>
  </xsl:template>

<xsl:template match="osis:verse[@sID]">

  <!-- <xsl:if test="not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])"> -->

  <xsl:variable name="chapter" select="substring-before(substring-after(@sID, '.'), '.')"/>
  <xsl:variable name="verse" select="@n"/>

	<xsl:variable name="chapterid" select="concat($bookid, '.', $chapter)"/>
	<xsl:variable name="verseid" select="concat($bookid, '.', $chapter, '.', $verse)"/>

	<!-- JohnT: insert chapter/verse anchor unless V1. The function removes CnV1 anchors from ranges.-->
	<xsl:choose>
		<xsl:when test="substring-before(@n,'-') = ''">
      <xsl:if test ="@n!='1'">
			<xsl:variable name="anchor" select="concat('C', $chapter, 'V', $verse)"/>
			  <a name="{$anchor}"/>
      </xsl:if>
    </xsl:when>
		<xsl:otherwise>
			<xsl:variable name="firstVerse" select="substring-before(@n,'-')"/>
			<xsl:variable name="lastVerse" select="substring-after(@n, '-')"/>
			<xsl:value-of select="user:anchors($firstVerse, $lastVerse, $chapter)" disable-output-escaping="yes"/>
			<!--xsl:variable name="anchor" select="concat('C', $chapter, 'V', $firstVerse)"/>
			<a name="{$anchor}"/-->
		</xsl:otherwise>
	</xsl:choose>

  <xsl:choose>
    <xsl:when test="substring-before(@sID, '.') = 'AddEsth'">
      <xsl:if test="substring-before(substring-after(@sID, '.'), '.') != substring-before(substring-after(preceding::osis:verse[@sID][1]/@sID, '.'), '.')">

		  <span class="chapter" id="{$chapterid}"> <xsl:value-of select="$chapter"/> </span>
      </xsl:if>
    </xsl:when>
    <xsl:when test="not($bookid='Obad' or $bookid='EpJer' or $bookid='PrMan' or $bookid='Phlm' or $bookid='2John' or $bookid='3John' or $bookid='Jude')">
      <xsl:if test="@n = '1' or starts-with(@n, '1-')">
        <span class="chapter" id="{$chapterid}"> <xsl:value-of select="$chapter"/> </span>
      </xsl:if>
    </xsl:when>
  </xsl:choose>

  <span class="verse" id="{$verseid}"><xsl:value-of select="@n"/></span>

  <!-- </xsl:if> -->

</xsl:template>



<xsl:template match="osis:title">

  <xsl:apply-templates/>

</xsl:template>



<xsl:template match="osis:divineName">

  <span class="smallcaps"><xsl:apply-templates/></span>

</xsl:template>

	<xsl:template match="osis:note[@type='x-quoteSource']"></xsl:template>
	<xsl:template match="osis:note[@type='x-quoteSource']" mode ="quoteSource">
		<div class="quoteSource">
			<xsl:apply-templates/>
		</div>
	</xsl:template>

	<!-- JohnT modified this note handling, to restart at chapters and add notes at end. -->
<xsl:template match="osis:note">

	<a>
		<xsl:attribute name="href">#FN<xsl:number format="a" level="any"/></xsl:attribute>
		<xsl:attribute name="onclick">hilite('FN<xsl:number format="a" level="any"/>')</xsl:attribute>
		<xsl:if test="@type='crossReference'">
			<span class="crmark">
				<xsl:number count="osis:note[@type='crossReference']" format="a" level="any" from="osis:chapter"/>
			</span>
			<span class="crpopup">
				<xsl:apply-templates mode="tooltip"/>
			</span>
		</xsl:if>
		<xsl:if test="not(@type='crossReference')">
			<span class="notemark">
				<xsl:number count="osis:note[not(@type='crossReference')]" format="a" level="any" from="osis:chapter"/>
			</span>
			<span class="popup">
				<xsl:apply-templates mode="tooltip"/>
			</span>
		</xsl:if>
	</a>

</xsl:template>


	<xsl:template match="osis:note" mode="endnotes">

		<p>
			<xsl:if test="@type='crossReference'">
				<xsl:attribute name="class">crossRefNote</xsl:attribute>
			</xsl:if>
			<xsl:if test="not(@type='crossReference')">
				<xsl:attribute name="class">footnote</xsl:attribute>
			</xsl:if>
				<xsl:attribute name="id">FN<xsl:number format="a" level="any"/></xsl:attribute>

			<xsl:if test="@type='crossReference'">
				<span class="crmark">
					<xsl:number count="osis:note[@type='crossReference']" format="a" level="any" from="osis:chapter"/>
				</span>
			</xsl:if>
			<xsl:if test="not(@type='crossReference')">
				<span class="notemark">
					<xsl:number count="osis:note[not(@type='crossReference')]" format="a" level="any" from="osis:chapter"/>
				</span>
			</xsl:if>
			<xsl:apply-templates mode="endnotes"/>
		</p>
	</xsl:template>

<xsl:template match="osis:reference" mode="endnotes">
	<!--This select was very hard to discover. It finds the value of the sID attribute on the nearest preceding
	verse element that has one (note that typically half the verse elements have eID instead).
	preceding::osis:verse[@sID] finds the verse elements before the current position that have sID attributes.
	The [1] selects just the closest...without that it seems to prefer the first in the whole document.
	Finally the additional /@sID obtains the value of the attribute (the first @sID is just part of the pattern
	that selects the element we want).-->
	<xsl:variable name="sID" select="preceding::osis:verse[@sID][1]/@sID"/>
	<xsl:choose>
		<xsl:when test="substring-after($sID,'.')=''">
			<!-- rarely, typicallly in a heading, our the select from our apply-templates doesn't include a preceding verse.
			In that case, take the less reliable approach of using the contents of the back reference.-->
			<xsl:variable name="xID" select="string(.)"/>
			<xsl:variable name="chapter" select="substring-before($xID, ':')"/>
			<xsl:variable name="verse" select="substring-after($xID, ':')"/>

			<xsl:choose>
				<xsl:when test="substring-before($verse,'-') = ''">
					<xsl:variable name="anchor" select="concat('C', $chapter, 'V', $verse)"/>
					<a class="noteBackRef" href="#{$anchor}"><xsl:apply-templates/>:</a>
				</xsl:when>
				<xsl:otherwise>
					<xsl:variable name="firstVerse" select="substring-before($verse,'-')"/>
					<xsl:variable name="anchor" select="concat('C', $chapter, 'V', $firstVerse)"/>
					<a class="noteBackRef" href="#{$anchor}"><xsl:apply-templates/>:</a>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:when>
		<xsl:otherwise>
			<!--<xsl:variable name="sID" select="string(.)"/> fails if ref content does not exactly match, e.g., 6:23b tries to find C6V23b but no b in the anchor-->
			<xsl:variable name="chapter" select="substring-before(substring-after($sID, '.'), '.')"/>
			<xsl:variable name="verse" select="preceding::osis:verse[@sID][1]/@n"/>

			<xsl:choose>
				<xsl:when test="substring-before($verse,'-') = ''">
					<xsl:variable name="anchor" select="concat('C', $chapter, 'V', $verse)"/>
					<a class="noteBackRef" href="#{$anchor}"><xsl:apply-templates/>:</a>
				</xsl:when>
				<xsl:otherwise>
					<xsl:variable name="firstVerse" select="substring-before($verse,'-')"/>
					<xsl:variable name="anchor" select="concat('C', $chapter, 'V', $firstVerse)"/>
					<a class="noteBackRef" href="#{$anchor}"><xsl:apply-templates/>:</a>
				</xsl:otherwise>
			</xsl:choose>

		</xsl:otherwise>
	</xsl:choose>
	<xsl:text> </xsl:text>

</xsl:template>
	<xsl:template match="osis:reference" mode="tooltip">
	</xsl:template>

<xsl:template match="osis:foreign">

  <i><xsl:apply-templates/></i>

</xsl:template>



<xsl:template match="osis:hi[@type='x-additional']">

  <i><xsl:apply-templates/></i>

</xsl:template>



<xsl:template match="osis:hi[@type='x-selah']">

  <i><xsl:apply-templates/></i>

</xsl:template>

<xsl:template match="osis:hi[@type='emphasis']">

  <i><xsl:apply-templates/></i>

</xsl:template>

<xsl:template match="osis:hi[@type='italic']">

	<i><xsl:apply-templates/></i>

</xsl:template>
	
<xsl:template match="osis:hi[@type='small-caps']">
	<span class="smallcaps"><xsl:apply-templates/></span>
</xsl:template>
	
<xsl:template match="osis:div[@type='x-highLevelPoetryDivision']">

  <div style="margin-left: 4cm"><xsl:apply-templates/></div>

</xsl:template>



<xsl:template match="osis:speaker">

  <div><i><xsl:apply-templates/></i></div>

</xsl:template>



<xsl:template match="osis:milestone[@type='x-blankLine']">

  <br/>

</xsl:template>



<xsl:template match="osis:lb">

  <!--
  <xsl:if test="not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])">
    <br/>
  </xsl:if>
  -->
  <br/>

</xsl:template>



<!--
<xsl:template match="text()">

  <xsl:if test="not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])">
    <xsl:value-of select="."/>
  </xsl:if>

</xsl:template>
-->


</xsl:stylesheet>
