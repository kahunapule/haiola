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
]>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0">
    <xsl:output method="xml"/>
    <xsl:output indent="no" />
    <xsl:param name="listOfBookNames">listOfBookNames.html</xsl:param>
    <xsl:param name="listOfBookNamesStartingWithDigit">listOfBookNamesStartingWithDigit.html</xsl:param>
    <xsl:param name="BookNamesWithPeriodAndWithout">BookNamesWithPeriodAndWithout.html</xsl:param>
    
    <!--   I hard coded the params -->
    
    
    <xsl:variable name="documentBookNamesWithPeriodAndWithout" select="document($BookNamesWithPeriodAndWithout)"/>
    <xsl:variable name="allowedBooksAbbreviationShortLong"
        select="document($listOfBookNames)/html/body/p/substring(text(),1,string-length(text())-1)"/>
    
<!-- chapter and verse need to be reversed -->
    <xsl:template
        match="test|note|char[@style='xt']|char[@style='xdc']|para[@style='r']|x|f|ft|fe|th|thr|tr|tc|cs[@sfm='rq']|rq|p[starts-with(@sfm,'i')]|p[starts-with(@sfm,'toc')]|p[@sfm='pc']|p[@sfm='rq']|p[@sfm='r']|ex|qot|qnt|xot_refs|xnt_refs|xdc_refs|xt|xtSee|xtSeeAlso|ef|esb">
        <!-- test is for testing the sample file -->
        <!-- usx styles follow -->
        <!-- starting with x are usfx styles -->
        
        <xsl:choose>
            <!-- ignore top level when there is a lower level present otherwise use top level -->
            <xsl:when test="child::*">
                <!-- usx style only needed for sample file where /test/ is element above  -->
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
    <xsl:otherwise>
        <xsl:element name="{local-name()}">
            <xsl:copy-of select="@*"/>
            <xsl:call-template name="fixChapterVerseError">
                <xsl:with-param name="string" select="."/>
            </xsl:call-template>
        </xsl:element>
        
    </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
    
    <xsl:template name="fixChapterVerseError">
        <xsl:param name="string" select="*"/>
        <xsl:analyze-string select="$string"
            regex="(&rlm;?[,;&arabicSemicolon;&arabicComma;])*(\s)?({$allowedBooksAbbreviationShortLong})?(\.)?(&rlm;?)([&nbsp;\s])?([0-9]+)(&rlm;?)([\.:,&arabicColon;&arabicComma;&mdash;&nbsp;-])?([0-9]+)?([a-z]*)(&rlm;?)([\.:&arabicColon;&mdash;&nbsp;-])?([0-9]+)?([a-z]*)?(&rlm;?)([\.:&arabicColon;&mdash;&nbsp;-])?([0-9]+)?([a-z]*)">
            <xsl:matching-substring>
                <!-- human reference start -->
    <!--  111<xsl:value-of select="regex-group(1)"/>
                222<xsl:value-of select="regex-group(2)"/>
                333<xsl:value-of select="regex-group(3)"/>
                444<xsl:value-of select="regex-group(4)"/>
                555<xsl:value-of select="regex-group(5)"/>
                666<xsl:value-of select="regex-group(6)"/>
                777<xsl:value-of select="regex-group(7)"/>
                888<xsl:value-of select="regex-group(8)"/>
                999<xsl:value-of select="regex-group(9)"/>
                1010<xsl:value-of select="regex-group(10)"/>
                1111<xsl:value-of select="regex-group(11)"/>
                1212<xsl:value-of select="regex-group(12)"/>
                1313<xsl:value-of select="regex-group(13)"/>
                1414<xsl:value-of select="regex-group(14)"/>
                1515<xsl:value-of select="regex-group(15)"/>
                1616<xsl:value-of select="regex-group(16)"/>
                1717<xsl:value-of select="regex-group(17)"/>
                1818<xsl:value-of select="regex-group(18)"/>
      -->
                <!-- human reference end -->
                <xsl:text> </xsl:text>
                <xsl:text>&rlm;</xsl:text>
                <xsl:value-of select="regex-group(1)"/>
                <xsl:text>&rlm;</xsl:text>
                <xsl:value-of select="regex-group(2)"/>
                <xsl:text>&rlm;</xsl:text>
                <xsl:value-of select="regex-group(3)"/>
                <xsl:text> </xsl:text>
                <xsl:text>&rlm;</xsl:text>
                <xsl:value-of select="regex-group(10)"/>
                <xsl:text>&rlm;</xsl:text>
                <xsl:value-of select="regex-group(9)"/>
                <xsl:text>&rlm;</xsl:text>
                <xsl:value-of select="regex-group(7)"/>
                <xsl:text>&rlm;</xsl:text>
                <xsl:value-of select="regex-group(13)"/>
                <xsl:text>&rlm;</xsl:text>
                <xsl:value-of select="regex-group(18)"/>
                <xsl:text>&rlm;</xsl:text>
                <xsl:value-of select="regex-group(17)"/>
                <xsl:text>&rlm;</xsl:text>
                <xsl:value-of select="regex-group(14)"/>
                <xsl:text>&rlm;</xsl:text>
                <xsl:value-of select="regex-group(19)"/>
                <xsl:text>&rlm;</xsl:text>
                <xsl:value-of select="regex-group(20)"/>
                <xsl:text>&rlm;</xsl:text>
                
                
                
            </xsl:matching-substring>    
            <xsl:non-matching-substring>
                <xsl:value-of select="."/>
            </xsl:non-matching-substring>
        </xsl:analyze-string>
    </xsl:template>
            

    <!-- identity transform -->
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

    <xsl:template match="@xml:space"/>

</xsl:stylesheet>
