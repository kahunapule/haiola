<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0">


    <!-- fix OBA 8.1 error -->
    <!-- identity transform -->
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>
    <xsl:template match="output">
        <xsl:copy-of select="."/>
    </xsl:template>
    
    <!-- Find single chapter books and then see if chapter number > 1 if so switch chapter and verse numbers -->
    <xsl:template match="ref">
        <xsl:variable name="tokenizedCurrentTarget" select="tokenize(@tgt,'\.')"/>
        <xsl:variable name="SILbookCode" select="$tokenizedCurrentTarget[1]"/>
        <xsl:variable name="chapterNumber" select="$tokenizedCurrentTarget[2]"/>
        <xsl:variable name="verseNumber" select="$tokenizedCurrentTarget[3]"/>
        <xsl:variable name="bookHasOnlyOneChapter"
            select="boolean($SILbookCode='OBA' or $SILbookCode='PHM' or $SILbookCode='2JN' or $SILbookCode='3JN' or $SILbookCode='JUD')"/>
        <xsl:choose>
            <xsl:when test="$bookHasOnlyOneChapter=true()">
                <xsl:choose>
                    <xsl:when test="$chapterNumber&gt;'1'">
                        <!-- chapter number greater than 1 in single chapter book-->
                        <xsl:element name="ref">
                            <xsl:attribute name="tgt">
                                <xsl:value-of select="$SILbookCode"/>
                                <xsl:text>.</xsl:text>
                                <xsl:value-of select="$verseNumber"/>
                                <xsl:text>.</xsl:text>
                                <xsl:value-of select="$chapterNumber"/>
                            </xsl:attribute>
                            <xsl:value-of select="."/>
                        </xsl:element>
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:copy-of select="."/>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
            <xsl:otherwise>
                <xsl:copy-of select="."/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>


</xsl:stylesheet>
