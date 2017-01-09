<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE stylesheet [
<!ENTITY cr "&#xD;&#xA;">
<!ENTITY tab "&#9;">
<!ENTITY nbsp "&#160;">
]>

<!-- history -->
<!-- need to match abbr with period and without so add two options one with period and one without-->

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0">
    <xsl:template match="BookNames">
        <BookNames>
            <xsl:apply-templates/>
        </BookNames>
    </xsl:template>
    <xsl:template match="book">
        <xsl:copy-of select="."/>
        <xsl:text>
</xsl:text>
        <xsl:variable name="removePeriod" select="replace(@abbr,'\.','')"/>
        <xsl:variable name="addPeriod" select="concat($removePeriod,'.')"/>
        <xsl:element name="book">
            <xsl:attribute name="code" select="@code"/>
            <xsl:attribute name="abbr" select="$addPeriod"/>
        </xsl:element>
        <xsl:text>
</xsl:text>
        <xsl:element name="book">
            <xsl:attribute name="code" select="@code"/>
            <xsl:attribute name="abbr" select="$removePeriod"/>
        </xsl:element>
        <xsl:text>
</xsl:text>
    </xsl:template>


</xsl:stylesheet>
