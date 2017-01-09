<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0">



    <!-- identity transform -->
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

    <!-- use XPath to look back and find preceding ref that has 3 letter code followed by a period. -->
    <xsl:template match="ref">
        <xsl:variable name="tokenizedCurrentTarget" select="tokenize(@tgt,'\.')"/>
        <!-- look back to find preceding ref to supply book name and or chapter number-->
        <xsl:variable name="precedingBookTarget"
            select="preceding-sibling::ref[@tgt[substring(.,4,1)='.']][1]/@tgt"/>
        <xsl:variable name="tokenizedPrecedingBookTarget"
            select="tokenize($precedingBookTarget,'\.')"/>
        <xsl:variable name="precedingChapterTarget"
            select="preceding-sibling::ref[not(contains(@tgt,'previousChapter'))][1]/@tgt"/>
        <xsl:variable name="tokenizedPrecedingChapterTarget"
            select="tokenize($precedingChapterTarget,'\.')"/>
        <xsl:choose>
            <!-- current target has book name -->
            <xsl:when test="string-length($tokenizedCurrentTarget[1])=3">
                <xsl:copy-of select="."/>
            </xsl:when>
            <!-- current target lacks book name and no preceding-sibling target book name -->
            <xsl:when
                test="($tokenizedCurrentTarget[1]) and not(preceding-sibling::ref[@tgt[substring(.,4,1)='.']][1]/@tgt)">
                <!-- remove ref -->
                <xsl:value-of select="."/>
            </xsl:when>
            <xsl:when
                test="($tokenizedCurrentTarget[1]) and not(preceding-sibling::ref[@tgt[substring(.,4,1)='.']][1]/@tgt)">
                <!-- remove ref -->
                <xsl:value-of select="."/>
            </xsl:when>


            <xsl:otherwise>
                <xsl:element name="ref">
                    <xsl:attribute name="tgt">
                                <xsl:choose>
                                    <xsl:when test="regex-group(7)">
                                        <!-- when chapter and verse are present -->
                                        <xsl:value-of select="$tokenizedCurrentTarget[3]"/>
                                    </xsl:when>
                                    <xsl:when test="contains(@tgt, 'previousChapterNumberHere')">
                                        <xsl:value-of select="$tokenizedPrecedingBookTarget[1]"/>
                                        <xsl:text>.</xsl:text>
                                        <xsl:value-of select="$tokenizedPrecedingChapterTarget[2]"/>
                                        <xsl:text>.</xsl:text>
                                        <xsl:value-of select="$tokenizedCurrentTarget[3]"/>
                                    </xsl:when>
                                    <xsl:when test="contains(@tgt, 'previousBookCodeHere')">
                                        <xsl:value-of select="$tokenizedPrecedingBookTarget[1]"/>
                                        <xsl:text>.</xsl:text>
                                        <xsl:value-of select="$tokenizedCurrentTarget[2]"/>
                                        <xsl:text>.</xsl:text>
                                        <xsl:value-of select="$tokenizedCurrentTarget[3]"/>
                                    </xsl:when>
                                    <xsl:otherwise>
                                        <xsl:value-of select="@tgt"/>
                                    </xsl:otherwise>
                                </xsl:choose>
                    </xsl:attribute>
                    <xsl:value-of select="."/>
                </xsl:element>
            </xsl:otherwise>
        </xsl:choose>

    </xsl:template>


</xsl:stylesheet>
