<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE stylesheet [
<!ENTITY cr "&#xD;&#xA;">
<!ENTITY tab "&#9;">
<!ENTITY nbsp "&#160;">
]>

<!-- history -->
<!-- need to match abbr with period and without so add two options one with period and one without-->
<!-- added مزمور for persian psalm 2015-05-01-->
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0">
    <xsl:template match="BookNames">
        <BookNames>
            <xsl:apply-templates/>
            <book code="PSA" abbr="Psalm" short="Salmo" long="Salm"/>
            <book code="PSA" abbr="مزمور" short="Salmo" long="Salm"/>
        </BookNames>
        <!-- add singular Psalm/Salmo -->

    </xsl:template>
    <xsl:template match="book">
        <xsl:element name="book">
            <xsl:attribute name="code" select="@code"/>
            <xsl:choose>
                <xsl:when test="@abbr">
                    <xsl:attribute name="abbr">
                        <xsl:call-template name="processAttributes">
                            <xsl:with-param name="rawText" select="@abbr"/>
                        </xsl:call-template>
                    </xsl:attribute>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:attribute name="abbr"/>
                </xsl:otherwise>
            </xsl:choose>
            <xsl:choose>
                <xsl:when test="@short">
                    <xsl:attribute name="short">
                        <xsl:call-template name="processAttributes">
                            <xsl:with-param name="rawText" select="@short"/>
                        </xsl:call-template>
                    </xsl:attribute>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:attribute name="short"/>
                </xsl:otherwise>
            </xsl:choose>
            <xsl:choose>
                <xsl:when test="@long">
                    <xsl:attribute name="long">
                        <xsl:call-template name="processAttributes">
                            <xsl:with-param name="rawText" select="@long"/>
                        </xsl:call-template>
                    </xsl:attribute>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:attribute name="long"/>
                </xsl:otherwise>
            </xsl:choose>
            <xsl:choose>
                <xsl:when test="@alt">
                    <xsl:attribute name="alt">
                        <xsl:call-template name="processAttributes">
                            <xsl:with-param name="rawText" select="@alt"/>
                        </xsl:call-template>
                    </xsl:attribute>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:attribute name="alt"/>
                </xsl:otherwise>
            </xsl:choose>
        </xsl:element>
    </xsl:template>

    <xsl:template name="processAttributes">
        <xsl:param name="rawText"/>
        <xsl:analyze-string select="$rawText" regex="(.+)(\()(.+)(\))">
            <!--
            <xsl:matching-substring>
            <xsl:value-of select="regex-group(1)"/>
            <xsl:choose>
                <xsl:when test="regex-group(2)">
                    <xsl:text> | </xsl:text>
                    <xsl:value-of select="regex-group(3)"/>
                </xsl:when>
                <xsl:otherwise>
                </xsl:otherwise>  
            </xsl:choose>
        </xsl:matching-substring>        
      -->
            <xsl:matching-substring>
                <xsl:value-of select="regex-group(1)"/>
                <xsl:choose>
                    <xsl:when test="regex-group(2)">
                        <xsl:text>\</xsl:text>
                        <xsl:value-of select="regex-group(2)"/>
                        <xsl:value-of select="regex-group(3)"/>
                        <xsl:text>\</xsl:text>
                        <xsl:value-of select="regex-group(4)"/>
                    </xsl:when>
                    <xsl:otherwise> </xsl:otherwise>
                </xsl:choose>
            </xsl:matching-substring>
            <xsl:non-matching-substring>
                <xsl:value-of select="$rawText"/>
            </xsl:non-matching-substring>
        </xsl:analyze-string>
    </xsl:template>
</xsl:stylesheet>
