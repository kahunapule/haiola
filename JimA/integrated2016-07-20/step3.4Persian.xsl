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
<!-- 2015-05-08 previous xslt does not catch Psalm 18 -->
<!-- 2015-05-08 fix by making direct match for the 3 cases -->
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

<xsl:template match="p[@sfm='r']">
  
    <xsl:choose>
       <xsl:when test="contains(.,' ‏‏‏مزمور ‏‏‏18‏‏‏‏‏‏‏')">
           <p sfm="r">
               <ref tgt="PSA.18.1">( ‏‏‏مزمور ‏‏‏18‏‏‏‏‏‏‏)</ref>
           </p>
       </xsl:when>
       <xsl:when test="contains(.,' ‏‏‏مزمور ‏‏‏53‏‏‏‏‏‏‏')">
           <p sfm="r">
               <ref tgt="PSA.18.1">( ‏‏‏مزمور ‏‏‏53‏‏‏‏‏‏‏)</ref>
           </p>
       </xsl:when>
       <xsl:when test="contains(.,' ‏‏‏مزمور ‏‏‏14‏‏‏‏‏‏‏')">
           <p sfm="r">
               <ref tgt="PSA.18.1">( ‏‏‏مزمور ‏‏‏14‏‏‏‏‏‏‏)</ref>
           </p>
       </xsl:when>
       <xsl:otherwise>
       <xsl:copy-of select="."/> 
         </xsl:otherwise>
       </xsl:choose>
    
</xsl:template>

  
</xsl:stylesheet>
