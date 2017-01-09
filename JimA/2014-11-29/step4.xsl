<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0">


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
        <xsl:variable name="precedingBookTarget"
            select="preceding-sibling::ref[@tgt[substring(.,4,1)='.']][1]/@tgt"/>
        <xsl:variable name="tokenizedPrecedingBookTarget"
            select="tokenize($precedingBookTarget,'\.')"/>
        <xsl:variable name="bookNamePrecededByDigit"
            select="document('listOfBookNamesStartingWithDigit.html')/html/body/p/text()"/>
        <xsl:variable name="tokenizedBookNamePrecededByDigit"
            select="tokenize(substring($bookNamePrecededByDigit,2),',')"/>
        <xsl:variable name="currentBookName" select="tokenize(@tgt,'\.')[1]"/>

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
        <xsl:variable name="contentIsOnlyOneDigit"
            select=".='1' or .='2' or .='3' or .='4' or .='5' or .='6' or .='7' or .='8' or .='9'"/>
        <xsl:variable name="missingBookCode" select="starts-with(@tgt,'previousBookCodeHere')"/>
        <xsl:variable name="missingVerseNumber" select="$tokenizedCurrentTarget[3]='1'"/>
        <xsl:variable name="followingWord" select="substring(following-sibling::text()[1],1,20)"/>
        <xsl:variable name="precedingText" select="preceding-sibling::text()[1]"/>
        <xsl:variable name="lengthPrecedingText" select="string-length($precedingText)"/>
        <xsl:variable name="booleanLongerThan10Characters" select="$lengthPrecedingText &gt; 10"/>
        <xsl:variable name="booleanPreviousBookName"
            select="$currentBookName='previousBookCodeHere'"/>
        <xsl:variable name="precedingWord"
            select="substring($precedingText,$lengthPrecedingText - 7,7)"/>
        <xsl:variable name="precedingTwoCharacters"
            select="substring($precedingWord,string-length($precedingWord) - 1,2)"/>
        <xsl:variable name="precedingPunctuationForChapterIsPresent"
            select="contains($precedingTwoCharacters,';')"/>
        <!-- ???not single chapter book -->
        <xsl:choose>
            <!-- value-of select="." removes the markup so no ref -->
            <!-- copy-of select="." saves the markup ref -->
            <xsl:when
                test="$contentIsOnlyOneDigit and $missingBookCode  and $missingVerseNumber and $precedingPunctuationForChapterIsPresent">
                <!-- Mat 3:5-6; 4 -->
                <xsl:copy-of select="."/>
            </xsl:when>
            <!-- need to use style=intro xxxx instead of looking for chapter number -->
            <xsl:when test="(not(preceding::c[1]) or parent::*[starts-with(@style,'Intro')]) and $booleanPreviousBookName">
                <xsl:value-of select="."/>
            </xsl:when>
            <xsl:when test="$booleanLongerThan10Characters and $booleanPreviousBookName">
                <xsl:value-of select="."/>
                <!--  Oba. 1 single chapter book verse 1 -->
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
            <xsl:when
                test="$followingPunctuation and $contentIsOnlyOneDigit and $missingBookCode  and not(contains($precedingWord,'\.\s')) and $precedingWord">
                <!-- 1) and 2)  or (1) and (2) -->
                <xsl:value-of select="."/>
            </xsl:when>
            <xsl:when
                test="$contentIsOnlyOneDigit and $missingBookCode  and $missingVerseNumber and $precedingWord">
                <!-- isolated number  3 -->
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
            <xsl:when
                test="$contentIsOnlyOneDigit and (some $x in $tokenizedBookNamePrecededByDigit satisfies contains($followingWord,$x))">
                <!-- remove reference two 3 John where the 3 is selected-->
                <!-- watch out for nul group error as it is not trapped yet -->
                <xsl:value-of select="."/>
            </xsl:when>
            <xsl:when test="preceding::c[1]/@id&gt;0 and $currentBookName='previousBookCodeHere'">
                <xsl:copy-of select="."/>
            </xsl:when>
            <xsl:otherwise>
                <!-- copy-of select="." saves the markup  -->
                <xsl:copy-of select="."/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
</xsl:stylesheet>
