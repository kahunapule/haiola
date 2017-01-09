<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    exclude-result-prefixes="xs"
    version="2.0">
    
    <xsl:template match="ref[@verified='']" priority="2">
  <xsl:text>
      link not matching any link in 350,000 data base</xsl:text>
        <xsl:call-template name="showRef"   /> 
        
    </xsl:template>
    <xsl:template match="ref[@linkFromHereFound='no'] " priority="1">
        <xsl:text>
            no link from here found</xsl:text>
        <xsl:call-template name="showRef"   /> 
        
    </xsl:template>
    <xsl:template match="ref[@foundInThisBible='no']" priority='3'>
        <xsl:text>
            not found in this bible</xsl:text>
        <xsl:call-template name="showRef"   /> 
    </xsl:template>
    
    <xsl:template name="showRef">
        <!-- show bcv of ref -->
        <xsl:text>
</xsl:text>
        <xsl:value-of select="preceding::id[1]/@id"/>
        <xsl:text>.</xsl:text>
        <xsl:value-of select="preceding::c[1]/@id"/>
        <xsl:text>.</xsl:text>
        <xsl:value-of select="preceding::v[1]/@id"/>
        <xsl:text>: </xsl:text>
        <xsl:value-of select="@tgt"/>
        <xsl:text> </xsl:text>
        <xsl:value-of select="."/>
        <xsl:text>
</xsl:text>
        <xsl:value-of select="parent::*"/>
        
    </xsl:template>
    
    <xsl:template match="p">
        <xsl:apply-templates/>
    </xsl:template>
    
    <xsl:template match="text()"/>
  </xsl:stylesheet>