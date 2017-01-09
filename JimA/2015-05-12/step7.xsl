<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema" exclude-result-prefixes="xs" version="2.0">

<!-- not using this step any more jaa 2015 -->

    <!-- verify that jump destination equals one of the 350,000 known destinations -->
    <xsl:param name="destinations-file">../source-target.xml</xsl:param>

    <!--    <xsl:param name="destinations-file">../cross_references_small_sample-test.xml</xsl:param>
    <xsl:param name="destinations"
        select="document($destinations-file)/references/source/target/substring(text(),1,string-length(text())-1)"/>
    <xsl:key name="findSource" match="source" use="target"/>

-->
 <!--   <xsl:param name="destinations" select="document($destinations-file)/references/source"/>
    <xsl:param name="allValidBCV" select="$destinations/@bcv"/>
 -->
    <xsl:key name="validBCV" match="source" use="@bcv"/>
    <!--    <xsl:variable name="currentTargetBCV" select="@tgt"></xsl:variable>
        <xsl:for-each select="$destinations">
            <xsl:choose>
                <xsl:when test="key('valildBCV',$currentTargetBCV"></xsl:when>
            </xsl:choose> 
      -->
    <xsl:key name="verseInThisBible" match="book//v" use="@bcv"/>

    <xsl:template match="ref" priority="2">
        <!-- see if destination exists within this document -->
        <!--       aaa <xsl:value-of select="key('verseInThisBible',@tgt)/@bcv"/> bbb  
        ccc <xsl:value-of select="@tgt"/> ddd  
     -->
        <!-- look at tgt  is it equal to any of the destinations -->
        <!-- We use for-each to change context to the states.xml doc 
          because key(  ) only locates elements in the same document as the 
          context node -->
      <!-- if reference is not found in this Bible then skip -->
        
        <xsl:choose>
            <xsl:when test="key('verseInThisBible',@tgt)/@bcv=@tgt">
        <xsl:copy-of select="."/>
            
            
            </xsl:when>
            <xsl:otherwise>
      <!-- skip --> 
                <xsl:value-of select="."/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
        <!--
        
        <xsl:element name="ref">
            <xsl:variable name="current-target" select="@tgt"/>
            <xsl:variable name="currentBCV" select="preceding::v/@bcv"/>
            < ! - - at this BCV is the target reference found that equals the tgt? - - >
            <xsl:variable name="atThisBCVisTargetReferenceEqualTo-tgt">
                <xsl:choose>
                    <xsl:when test="$destinations[@bcv=$currentBCV]/target=@tgt">
                        <xsl:text>yes</xsl:text>
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:text>no</xsl:text>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:variable>
            <xsl:attribute name="tgt" select="@tgt"/>
            <xsl:attribute name="linkFromHereFound">
                <xsl:value-of select="$atThisBCVisTargetReferenceEqualTo-tgt"/>
            </xsl:attribute>
            < ! - - is the tgt found as a BCV in this work? - - >
            <xsl:attribute name="foundInThisBible">
            </xsl:attribute>

            < ! - - list all of the references found at this BCV - - >
            <xsl:attribute name="verified">
                <xsl:value-of select="$destinations[target=$current-target]/@bcv"/>
            </xsl:attribute>
            <xsl:value-of select="."/>
        </xsl:element>  
    </xsl:template>-->
    <!-- identity transform -->
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>


</xsl:stylesheet>
