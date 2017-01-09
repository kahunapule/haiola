<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema" exclude-result-prefixes="xs" version="2.0">
    <xsl:key name="validBCV" match="source" use="@bcv"/>
    <xsl:key name="verseInThisBible" match="book//v" use="@bcv"/>
    <xsl:template match="ref" priority="2">
        <xsl:choose>
            <xsl:when test="key('verseInThisBible',@tgt)/@bcv=@tgt">
        <xsl:copy-of select="."/>
            </xsl:when>
            <xsl:otherwise>
                <xsl:value-of select="."/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>
</xsl:stylesheet>