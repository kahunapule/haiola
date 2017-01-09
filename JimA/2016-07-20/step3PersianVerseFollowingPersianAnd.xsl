<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE stylesheet [
<!ENTITY cr "&#xD;&#xA;">
<!ENTITY tab "&#9;">
<!ENTITY nbsp "&#160;">
<!ENTITY mdash "&#8212;">
<!ENTITY ndash "&#8211;">
<!ENTITY lrm "&#8206;">
<!ENTITY rlm "&#8207;">
<!ENTITY arabicComma "،">
<!ENTITY arabicSemicolon "؛">
<!ENTITY arabicColon ":">
<!ENTITY arabicAnd "و">
]>

<!-- history -->
<!-- 2015-05-08 Persian /and/ followed by number is a verse number -->
<!-- 2015-05-08 split from step3.xsl -->
<!-- make sure period stays if it is in the abbreviation -->
<!-- 2014-12-21 require full bcv in front matter/intro -->
<!-- 2014-11-23 fixing error. if booknames.xml file has no period following abbreviation then allow either with or without period in text-->
<!-- 2014-11-07 fixing error. if booknames.xml file has period following abbreviation then allow either with or without period in text -->
<!-- 2014-10-03 fixing error. did not include enough possible paragraph styles to search for references -->
<!-- 2013-09-26 add \r support -->
<!-- 2013-10-03 add ltr support -->
<!-- 2013-10-10 made more generic to fix <k> disappearing problem -->
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0">
    <xsl:output method="xml"/>
    
    <!-- identity transform -->
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

    <xsl:template match="output">
        <xsl:copy-of select="."/>
    </xsl:template>

    <xsl:template match="ref">
   <xsl:variable name="parsedBCV" select="tokenize(@tgt,'.')"/>
        <!-- currently nothing happens in this xslt so it looks like it can be eliminated. -->
        <xsl:choose>
            <xsl:when test="preceding-sibling::*[1]='‏‏‏ و ‏‏ ‏ ‏'">  <!-- and $parsedBCV[1]='previousBookCodeHere' and $parsedBCV[3]='1'"> -->
      <xsl:element name="ref">
         <!-- move chpater number to verse number and get chapter number from preceding ref --> 
       <xsl:attribute name="jim">ssssssss</xsl:attribute>
          <xsl:attribute name="tgt">
              <xsl:value-of select="$parsedBCV[1]"/>
              <xsl:text>.</xsl:text>
              <xsl:call-template name="getPrecedingChaperNumber"/>
              <xsl:text>.</xsl:text>
          <xsl:value-of select="$parsedBCV[2]"/>    
          </xsl:attribute>
      </xsl:element>          
        
         </xsl:when>
            <xsl:otherwise>
                <xsl:copy-of select="."/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <xsl:template name="getPrecedingChaperNumber">
        <!--  <ref tgt="previousBookCodeHere.15.25">‏ 15‏:25‏-16‏:6</ref> -->
    <!-- so if there is a hyphen in the reference we must use the second chapter number -->
  <xsl:variable name="precedingReference" select="preceding::ref[1]"/>
             <xsl:call-template name="extractChapterNumber">
                 <xsl:with-param name="reference" select="$precedingReference"/>
             </xsl:call-template>
    </xsl:template>
    <xsl:template name="extractChapterNumber">
        <xsl:param name="reference"/>
        <xsl:variable name="tokenizedReference" select="tokenize($reference,':|-')"/>
        <xsl:choose>
            <xsl:when test="$tokenizedReference[3]">
                <xsl:value-of select="$tokenizedReference[3]"/>
            </xsl:when>
            <xsl:otherwise>
                <xsl:value-of select="$tokenizedReference[1]"/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
   
</xsl:stylesheet>
