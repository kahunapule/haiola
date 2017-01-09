<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0">


    <!-- identity transform -->
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

    <xsl:template match="ref">
        <xsl:variable name="precedingBookTarget"
            select="preceding-sibling::ref[@tgt[substring(.,4,1)='.']][1]/@tgt"/>
        <xsl:variable name="tokenizedPrecedingBookTarget"
            select="tokenize($precedingBookTarget,'\.')"/>
        <xsl:variable name="bookNamePrecededByDigit"
            select="document('../listOfBookNamesStartingWithDigit.html')/html/body/p/text()"/>
        <xsl:variable name="tokenizedBookNamePrecededByDigit"
            select="tokenize(substring($bookNamePrecededByDigit,2),',')"/>
        
        <xsl:variable name="tokenizedCurrentTarget" select="tokenize(@tgt,'\.')"/>
        <!-- look back to find preceding ref to supply book name and or chapter number-->
        <xsl:variable name="precedingBookTarget"
            select="preceding-sibling::ref[@tgt[substring(.,4,1)='.']][1]/@tgt"/>
        <xsl:variable name="tokenizedPrecedingBookTarget"
            select="tokenize($precedingBookTarget,'\.')"/>
        <xsl:variable name="precedingChapterTarget"
            select="preceding-sibling::ref[not(tokenize(@tgt,'\.')[2]='previouseChapterNumberHere')][1]/@tgt"/>
        <xsl:variable name="tokenizedPrecedingChapterTarget"
            select="tokenize($precedingChapterTarget,'\.')"/>
        <xsl:variable name="followingPunctuation"
            select="starts-with(following-sibling::text()[1],')')"/>
        <xsl:variable name="onlyOneDigit"
            select=".='1' or .='2' or .='3' or .='4' or .='5' or .='6' or .='7' or .='8' or .='9'"/>
        <xsl:variable name="missingBookCode" select="starts-with(@tgt,'previousBookCodeHere')"/>
        <xsl:variable name="followingWord" select="substring(following-sibling::text()[1],1,20)"/>
        <xsl:variable name="precedingText" select="preceding-sibling::text()[1]"/>
        <xsl:variable name="lengthPrecedingText" select="string-length($precedingText)"/>
        <xsl:variable name="precedingWord" select="substring($precedingText[1],1,7)"/>
        <xsl:choose>
            <!-- single chapter book -->
            <xsl:when
                test="$tokenizedPrecedingBookTarget[1]='JUD' or $tokenizedPrecedingBookTarget[1]='OBA' or $tokenizedPrecedingBookTarget[1]='PHM' or $tokenizedPrecedingBookTarget[1]='2JN' or $tokenizedPrecedingBookTarget[1]='3JN'">
                <xsl:element name="ref">
                    <xsl:attribute name="tgt">
                        <xsl:value-of select="$tokenizedPrecedingBookTarget[1]"/>
                        <xsl:text>.</xsl:text>
                        <xsl:text>1</xsl:text>
                        <xsl:text>.</xsl:text>
                        <xsl:value-of select="."/>
                    </xsl:attribute>
                    <xsl:value-of select="."/>
                </xsl:element>
            </xsl:when>
            <xsl:otherwise>
                <!-- not single chapter book -->
                <xsl:choose>
                    <!--            <xsl:when test="$tokenizedCurrentTarget[3]='1'">
                        < ! - - chapter only found, and it should be a verse number - - >
                        <xsl:text>.</xsl:text>
                        <xsl:value-of select="$tokenizedCurrentTarget[2]"/>
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:text>.</xsl:text>
                        <xsl:value-of select="."/>
                    </xsl:otherwise>
                 -->
                    <xsl:when test="$followingPunctuation and $onlyOneDigit and $missingBookCode ">
                        <!-- 1) and 2)  or (1) and (2) -->
                        <xsl:value-of select="."/>
                    </xsl:when>
                    <xsl:when test="number($tokenizedCurrentTarget[2]) &gt; 151">
                        <!-- remove reference  as no chapter over 151-->
                        <xsl:value-of select="."/>
                    </xsl:when>
                    <xsl:when test="number($tokenizedCurrentTarget[3]) &gt; 176">
                        <!-- remove reference as no verse over 176 (Psa 119.176)-->
                        <xsl:value-of select="."/>
                    </xsl:when>
                    <xsl:when test="$onlyOneDigit and (some $x in $tokenizedBookNamePrecededByDigit satisfies contains($followingWord,$x))">
                        <!-- remove reference two 3 John where the 3 is selectred-->
                        <!-- watch out for nul group error as it is not trapped yet -->
                        <xsl:value-of select="."/>
                    </xsl:when>
                    <xsl:when
                        test="$lengthPrecedingText &lt; 8 and contains($precedingWord,'and') and $tokenizedCurrentTarget[3]='1'">
                        <!-- switch chapter and verse-->
                        <xsl:element name="ref">
                            <xsl:attribute name="tgt">
                                <xsl:value-of select="$tokenizedCurrentTarget[1]"/>
                                <xsl:text>.</xsl:text>
                                <xsl:value-of select="$tokenizedPrecedingChapterTarget[2]"/>
                                <xsl:text>.</xsl:text>
                                <xsl:value-of select="$tokenizedCurrentTarget[2]"/>
                            </xsl:attribute>
                            <xsl:value-of select="."/>
                        </xsl:element>
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:copy-of select="."/>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
</xsl:stylesheet>
