<xsl:stylesheet version="1.0"
                xmlns:osis="http://www.bibletechnologies.net/2003/OSIS/namespace"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
				xmlns:msxsl="urn:schemas-microsoft-com:xslt"
				xmlns:user="urn:nowhere">
	
<xsl:output method="html" encoding="UTF-8" doctype-public="-//W3C//DTD XHTML 1.0 Transitional//EN" doctype-system="http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"/>


<!-- XSLT stylesheet for Bible text in OSIS format (output to HTML, with display.css file) -->
<!-- Dave van Grootheest, Netherlands Bible Society -->
<!-- last updated: 2007-07-12 -->



<!--
<xsl:param name="startId"/>
<xsl:param name="endId"/>
-->

<xsl:variable name="bookid" select="//osis:div[@type='book']/@osisID"/>
<xsl:variable name="bookname" select="//osis:div[@type='book']//osis:title[text()][1]"/>
<xsl:variable name="source" select="/osis:osis/osis:osisText/osis:header//osis:source"/>
<xsl:variable name="textversion0" select="concat(2, substring-before(substring-after($source, '(2'), ')'))"/>
<xsl:variable name="textversion" select="concat(substring($textversion0, 9), '-', substring($textversion0, 6, 2), '-', substring($textversion0, 1, 4))"/>



<xsl:template match="/">
	<html>
    <head>
      <link rel="stylesheet" href="display.css" type="text/css"/>
	  <script src="TextFuncs.js" type="text/javascript"></script>
	</head>
	<body onload="onLoad()">

      <xsl:apply-templates select="//osis:div[@type='book']"/>
		<div class="footnotes">
			<xsl:apply-templates select="//osis:note[not(@type='crossReference')]" mode="endnotes"/>
			<xsl:apply-templates select="//osis:note[@type='crossReference']" mode="endnotes"/>
		</div>

    </body>
  </html>

</xsl:template>

<xsl:template match="osis:hi[@type='emphasis']" mode="endnotes">

  <i><xsl:apply-templates/></i>

</xsl:template>

<xsl:template match="osis:osis">

  <xsl:apply-templates select="//osis:osisText"/>

</xsl:template>



<xsl:template match="osis:osisText">

  <xsl:apply-templates select="osis:div"/>

</xsl:template>



<xsl:template match="osis:div[@type='book']">

  <xsl:if test="osis:div[@type='section']">

    <xsl:choose>
      <xsl:when test="osis:title/osis:title[@level='2']">

		  <!-- JohnT: was this; but Chuck uses multiple level 2 titles, sometimes before level 1, e.g.
		The letter of Paul/to the church at/Ephesus
      <div class="maintitle1"><xsl:value-of select="osis:title/osis:title[@level='1']"/></div>
      <div class="maintitle2"><xsl:value-of select="osis:title/osis:title[@level='2']"/></div>
		-->
		  <xsl:apply-templates select="osis:title/osis:title" mode="title"/>
		  <div class="copyright">©2007 UBB-GMIT</div>
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
          <xsl:number/>
          <xsl:text>.</xsl:text>
        </td>
        <td>
          <xsl:choose>
            <xsl:when test="osis:title">
              <a href="#{position()}">
                <xsl:apply-templates select="osis:title"/>
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

  <xsl:if test="osis:p | osis:list | osis:lg | osis:lb | osis:milestone | osis:speaker | osis:div[@type='x-highLevelPoetryDivision'] | text()[.!='&#10;']">
    <div class="text">
	  <!-- JohnT: in the original this had no select. But then if the 'if' succeeds all the main sections get included twice!
	  This if appears to be to insert anything that precedes the main sections. I'm not sure how to get whatever might be matched by
	  the text()[.!='&#10;']-->
      <xsl:apply-templates select="osis:p | osis:list | osis:lg | osis:lb | osis:milestone | osis:speaker | osis:div[@type='x-highLevelPoetryDivision']"/>
    </div>
  </xsl:if>

  <!-- <xsl:apply-templates select="osis:div[@type='section' and not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])]"/> -->
  <xsl:apply-templates select="osis:div[@type='section']"/>

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
    <div class="sectionheading"><a name="{position()}"><xsl:value-of select="osis:title"/></a></div>
  </xsl:if>
	<xsl:if test="osis:title[@type='parallel']">
		<div class="parallel">
			<xsl:value-of select="osis:title[@type='parallel']"/>
		</div>
	</xsl:if>

  <div class="text">
    <!-- <xsl:apply-templates select="osis:p[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:list[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:lg[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:milestone[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:lb[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])]"/> -->
    <xsl:apply-templates select="osis:p | osis:list | osis:lg | osis:lb | osis:milestone | osis:speaker | osis:div[@type='x-highLevelPoetryDivision'] | text()[.!='&#10;']"/>
  </div>
	<!-- JohnT: This allows handling subsections embedded in divisions.-->
	<xsl:apply-templates select="osis:div"/>

</xsl:template>

<xsl:template match="osis:div[@type='subSection']">

	<!-- <xsl:if test="osis:title and starts-with(@scope, concat($startId, '-')) or osis:title and preceding::osis:verse[@sID = $startId]"> -->
	<xsl:if test="osis:title">
		<div class="sectionsubheading">
			<a name="{position()}">
				<xsl:value-of select="osis:title"/>
			</a>
		</div>
	</xsl:if>
	<xsl:if test="osis:title[@type='parallel']">
		<div class="parallelSub">
			<xsl:value-of select="osis:title[@type='parallel']"/>
		</div>
	</xsl:if>

	<div class="text">
		<!-- <xsl:apply-templates select="osis:p[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:list[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:lg[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:milestone[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:lb[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])]"/> -->
		<xsl:apply-templates select="osis:p | osis:list | osis:lg | osis:lb | osis:milestone | osis:speaker | osis:div[@type='x-highLevelPoetryDivision'] | text()[.!='&#10;']"/>
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



<xsl:template match="osis:lg">

  <xsl:if test="name(preceding-sibling::*[1])='lg'">
    <br/>
  </xsl:if>

  <xsl:apply-templates/>

</xsl:template>



<xsl:template match="osis:l">

  <div class="poetry">
    <xsl:apply-templates/>
  </div>

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
			for (i = firstVerse; i <= lastVerse; i++)
				result = result + "<a name=\"C" + chapter + "V" + i + "\"/>";
			return result;		}
		]]>
	</msxsl:script>

<xsl:template match="osis:verse[@sID]">

  <!-- <xsl:if test="not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])"> -->

  <xsl:variable name="chapter" select="substring-before(substring-after(@sID, '.'), '.')"/>
  <xsl:variable name="verse" select="@n"/>

	<xsl:variable name="chapterid" select="concat($bookid, '.', $chapter)"/>
	<xsl:variable name="verseid" select="concat($bookid, '.', $chapter, '.', $verse)"/>

	<!-- JohnT: insert chapter/verse anchor-->
	<!-- JohnT: insert chapter anchor-->

	<xsl:if test="@n = '1' or starts-with(@n, '1-')">
		<xsl:variable name="anchorC" select="concat('C', $chapter)"/>
		<a name="{$anchorC}"/>
	</xsl:if>
	<xsl:choose>
		<xsl:when test="substring-before(@n,'-') = ''">
			<xsl:variable name="anchor" select="concat('C', $chapter, 'V', $verse)"/>
			<a name="{$anchor}"/>
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


	<!-- JohnT modified this note handling, to restart at chapters and add notes at end. -->
<xsl:template match="osis:note">

	<a>
		<xsl:attribute name="href">#FN<xsl:number format="a" level="any"/></xsl:attribute>
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
		<span class="popup">
			<xsl:apply-templates mode="tooltip"/>
		</span>
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
		<a><xsl:attribute name="name">FN<xsl:number format="a" level="any"/></xsl:attribute></a>
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
	<xsl:template match="osis:note[@type='crossReference']" mode="endnotes">

		<p>
			<xsl:if test="@type='crossReference'">
				<xsl:attribute name="class">crossRefNote</xsl:attribute>
			</xsl:if>
			<xsl:if test="not(@type='crossReference')">
				<xsl:attribute name="class">footnote</xsl:attribute>
			</xsl:if>
			<a>
				<xsl:attribute name="name">FN<xsl:number format="a" level="any"/></xsl:attribute>
			</a>
			<span class="crmark">
				<xsl:number count="osis:note[@type='crossReference']" format="a" level="any" from="osis:chapter"/>
			</span>
			<xsl:apply-templates mode="endnotes"/>
		</p>

	</xsl:template>

<xsl:template match="osis:reference" mode="endnotes">
	<!--<xsl:variable name="sID" select="preceding::verse[@sID]"/> -->
	<xsl:variable name="sID" select="string(.)"/>
	<xsl:variable name="chapter" select="substring-before($sID, ':')"/>
	<xsl:variable name="verse" select="substring-after($sID, ':')"/>
	
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
