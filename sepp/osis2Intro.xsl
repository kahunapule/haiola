<xsl:stylesheet version="1.0"
                xmlns:osis="http://www.bibletechnologies.net/2003/OSIS/namespace"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
				xmlns:msxsl="urn:schemas-microsoft-com:xslt"
				xmlns:user="urn:nowhere">
	
<xsl:output method="html" encoding="UTF-8" doctype-public="-//W3C//DTD XHTML 1.0 Transitional//EN" doctype-system="http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"/>


<!-- XSLT stylesheet for Bible text in OSIS format (Extracts Introductin sections to HTML, with display.css file) -->
<!-- John Thomson, based on a template by Dave van Grootheest, Netherlands Bible Society -->
<!-- last updated: 2008-Nov-2 -->


<xsl:param name="copyright"/>

<xsl:variable name="bookid" select="//osis:div[@type='book']/@osisID"/>
<xsl:variable name="bookname" select="//osis:div[@type='book']//osis:title[text()][1]"/>
<xsl:variable name="source" select="/osis:osis/osis:osisText/osis:header//osis:source"/>
<xsl:variable name="textversion0" select="concat(2, substring-before(substring-after($source, '(2'), ')'))"/>
<xsl:variable name="textversion" select="concat(substring($textversion0, 9), '-', substring($textversion0, 6, 2), '-', substring($textversion0, 1, 4))"/>

<xsl:template match="/">
	<xsl:if test="//osis:div[@type='introduction']">
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
		<!--JohnT: IE seems to cut off the last line; insert a blank.-->
		<div>
			<br/>
		</div>

	</body>
  </html>
	</xsl:if>
</xsl:template>

<xsl:template match="osis:div[@type='book' or @type='majorSection']">

	<xsl:apply-templates select="osis:div[@type='introduction']"/>

</xsl:template>


<xsl:template match="osis:div[@type='introduction']">
	<div class="introduction">
		<xsl:apply-templates select="osis:p | osis:div "/>
	</div>
</xsl:template>

<xsl:template match="osis:div">
	<xsl:apply-templates/>
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
		<xsl:when test="@level>1">
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

  <div class="introListItem">
    <xsl:apply-templates/>
  </div>

</xsl:template>


<xsl:template match="osis:title">

  <xsl:apply-templates/>

</xsl:template>



<xsl:template match="osis:divineName">

  <span class="smallcaps"><xsl:apply-templates/></span>

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


</xsl:stylesheet>
