<xsl:stylesheet version="1.0"
                xmlns:osis="http://www.bibletechnologies.net/2003/OSIS/namespace"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:output method="html" encoding="UTF-8"/>


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
    </head>
    <body>

      <xsl:apply-templates select="//osis:div[@type='book']"/>

    </body>
  </html>

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

        <div class="maintitle1"><xsl:value-of select="osis:title/osis:title[@level='1']"/></div>
        <div class="maintitle2"><xsl:value-of select="osis:title/osis:title[@level='2']"/></div>

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

      <div class="maintitle1"><xsl:value-of select="osis:title/osis:title[@level='1']"/></div>
      <div class="maintitle2"><xsl:value-of select="osis:title/osis:title[@level='2']"/></div>

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
      <xsl:apply-templates/>
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



<xsl:template match="osis:div[@type='section']">

  <!-- <xsl:if test="osis:title and starts-with(@scope, concat($startId, '-')) or osis:title and preceding::osis:verse[@sID = $startId]"> -->
  <xsl:if test="osis:title">
    <div class="sectionheading"><a name="{position()}"><xsl:value-of select="osis:title"/></a></div>
  </xsl:if>

  <div class="text">
    <!-- <xsl:apply-templates select="osis:p[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:list[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:lg[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:milestone[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])] | osis:lb[not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])]"/> -->
    <xsl:apply-templates select="osis:p | osis:list | osis:lg | osis:lb | osis:milestone | osis:speaker | osis:div[@type='x-highLevelPoetryDivision'] | text()[.!='&#10;']"/>
  </div>

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



<xsl:template match="osis:verse[@sID]">

  <!-- <xsl:if test="not(following::osis:verse[@sID = $startId] or preceding::osis:verse[@eID = $endId])"> -->

  <xsl:variable name="chapter" select="substring-before(substring-after(@sID, '.'), '.')"/>
  <xsl:variable name="verse" select="@n"/>

  <xsl:variable name="chapterid" select="concat($bookid, '.', $chapter)"/>
  <xsl:variable name="verseid" select="concat($bookid, '.', $chapter, '.', $verse)"/>


  <xsl:choose>
    <xsl:when test="substring-before(@sID, '.') = 'AddEsth'">
      <xsl:if test="substring-before(substring-after(@sID, '.'), '.') != substring-before(substring-after(preceding::osis:verse[@sID][1]/@sID, '.'), '.')">
      
        <span class="chapter" id="{$chapterid}"> [<xsl:value-of select="$chapter"/>] </span>
      </xsl:if>
    </xsl:when>
    <xsl:when test="not($bookid='Obad' or $bookid='EpJer' or $bookid='PrMan' or $bookid='Phlm' or $bookid='2John' or $bookid='3John' or $bookid='Jude')">
      <xsl:if test="@n = '1' or starts-with(@n, '1-')">
        <span class="chapter" id="{$chapterid}"> [<xsl:value-of select="$chapter"/>] </span>
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



<xsl:template match="osis:note">

  <!-- <span class="notemark">[noot] </span> -->

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
