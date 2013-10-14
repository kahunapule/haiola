<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE stylesheet [
<!ENTITY cr "&#xD;&#xA;">
<!ENTITY tab "&#9;">
<!ENTITY nbsp "&#160;">
<!ENTITY mdash "&#8212;">
<!ENTITY ndash "&#8211;">

]>

<!-- history -->
<!-- 2013-09-26 add \r support -->

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0">
    <xsl:output method="xml"/>



    <xsl:variable name="documentName" select="document('../BookNames.xml')"/>
    <xsl:variable name="allowedBooksAbbrevationShortLong"
        select="document('../listOfBookNames.html')/html/body/p"/>

    <xsl:key name="abbr2book" match="book" use="@abbr"/>
    <xsl:key name="abbr2book" match="book" use="@short"/>
    <xsl:key name="abbr2book" match="book" use="@long"/>
    <xsl:key name="abbr2book" match="book" use="@alt"/>

    <!-- identity transform -->
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

    <xsl:template
        match="test|x|f|fe|th|thr|tr|tc|cs[@sfm='rq']|p[starts-with(@sfm,'i')]|p[@sfm='pc']|p[@sfm='r']|ex|qot|qnt|xot_refs|xnt_refs|xdc_refs|xt|xtSee|xtSeeAlso|ef|esb">
        <xsl:choose>
            <xsl:when test="child::x">
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
            <!-- xo shows where cross-reference is located --> 
            <xsl:when test="child::xo">
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
            <xsl:when test="child::xt">
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
           <!-- footnote reference shows where footnote is located --> 
            <xsl:when test="child::fr">
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
            <xsl:otherwise>
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:call-template name="hyperlink">
                        <xsl:with-param name="string" select="."/>
                    </xsl:call-template>
                </xsl:element>
            </xsl:otherwise>

        </xsl:choose>
    </xsl:template>

    <xsl:template name="hyperlink">
        <xsl:param name="string" select="string(.)"/>

        <!--    regex="([;,])?(\s)?({$allowedBooksAbbrevationShortLong})?\.?([&nbsp;\s])?([0-9]+)([\.:,&mdash;&nbsp;-])?([0-9]+)?([a-z]*)([\.:,&mdash;&nbsp;-])?([0-9]+)?([a-z]*)?">-->
        <!--         1 initial comma or ;/ 2 space/ 3 book name or abbrevation / not followed by capital letter like 1Co   4 space /  5 chapter or verse number / 6 punctuation / 7 verse number/ 8 abff / 9 verse number / 10 abff /                                                                                                                  -->
        <xsl:analyze-string select="$string"
            regex="([;,])?(\s)?({$allowedBooksAbbrevationShortLong})?\.?([&nbsp;\s])?([0-9]+)([\.:,&mdash;&nbsp;-])?([0-9]+)?([a-z]*)([\.:&mdash;&nbsp;-])?([0-9]+)?([a-z]*)?([\.:&mdash;&nbsp;-])?([0-9]+)?([a-z]*)">
            <xsl:matching-substring>
                <!--                    <xsl:text>

                </xsl:text>
               <xsl:value-of select="$string"/>
                <xsl:text>
</xsl:text>
                <xsl:text> 1 </xsl:text>
                <xsl:value-of select="regex-group(1)"/>
                <xsl:text> 2</xsl:text>
                <xsl:value-of select="regex-group(2)"/>
                <xsl:text> 3</xsl:text>
                <xsl:value-of select="regex-group(3)"/>
                <xsl:text> 4</xsl:text>
                <xsl:value-of select="regex-group(4)"/>
                <xsl:text> 5</xsl:text>
                <xsl:value-of select="regex-group(5)"/>
                <xsl:text> 6</xsl:text>
                <xsl:value-of select="regex-group(6)"/>
                <xsl:text> 7</xsl:text>
                <xsl:value-of select="regex-group(7)"/>
                <xsl:text> 8</xsl:text>
                <xsl:value-of select="regex-group(8)"/>
                <xsl:text> 9</xsl:text>
                <xsl:value-of select="regex-group(9)"/>
                <xsl:text> 10</xsl:text>
                <xsl:value-of select="regex-group(10)"/>
                <xsl:text> 11</xsl:text>
                <xsl:value-of select="regex-group(11)"/>
                <xsl:text> 12</xsl:text>
                <xsl:value-of select="regex-group(12)"/>
                <xsl:text> 13</xsl:text>
                <xsl:value-of select="regex-group(13)"/>
                <xsl:text> 14</xsl:text>
                <xsl:value-of select="regex-group(14)"/>
                <xsl:text> 15</xsl:text>
                <xsl:value-of select="regex-group(15)"/>
                <xsl:text> 16</xsl:text>
                <xsl:value-of select="regex-group(16)"/>
                <xsl:text>
</xsl:text>
       -->
                <!-- variables -->


                <xsl:variable name="bookNamePresent" select="boolean(regex-group(3))"/>
                <xsl:variable name="chapterVerseSeparatorPresent">
                    <xsl:value-of
                        select="boolean(regex-group(6)='.') or boolean(regex-group(6)=':') or boolean(regex-group(6)=',') "
                    />
                </xsl:variable>
                <xsl:variable name="chapterBridgePresent">
                    <xsl:value-of
                        select="boolean(regex-group(6)='&mdash;') or boolean(regex-group(6)='&nbsp;') or boolean(regex-group(6)='-') "
                    />
                </xsl:variable>
                <xsl:variable name="semicolonPreceedsReference" select="boolean(regex-group(1)=';')"/>
                <xsl:variable name="verseNumberPresent" select="boolean(regex-group(7))"/>
                <xsl:variable name="bookName" select="regex-group(3)"/>
                <xsl:variable name="verseNumber" select="regex-group(7)"/>
                <xsl:variable name="SILbookCode">
                    <xsl:call-template name="convertBookName2SILBook">
                        <xsl:with-param name="bookName">
                            <!--  remove a trailing period from lookup text -->
                            <xsl:value-of select="replace(regex-group(3),'\.','')"/>
                        </xsl:with-param>
                    </xsl:call-template>
                </xsl:variable>
                <xsl:variable name="bookHasOnlyOneChapter"
                    select="boolean($SILbookCode='OBA' or $SILbookCode='PHM' or $SILbookCode='2JN' or $SILbookCode='3JN' or $SILbookCode='JUD')"/>


                <xsl:variable name="chapterNumber">
                    <xsl:choose>
                        <xsl:when test="$bookHasOnlyOneChapter=true()">
                            <xsl:text>1</xsl:text>
                        </xsl:when>
                        <xsl:when test="regex-group(1)=','">
                            <xsl:choose>
                                <xsl:when test="not(regex-group(7)) and  $SILbookCode='PSA'">
                                    <!-- series of chapters in Psalms -->
                                    <xsl:value-of select="regex-group(5)"/>
                                </xsl:when>
                                <xsl:when
                                    test="regex-group(5) and regex-group(7) and regex-group(6)='.'">
                                    <xsl:value-of select="regex-group(5)"/>
                                </xsl:when>
                                <xsl:when
                                    test="regex-group(5) and regex-group(7) and regex-group(6)=':'">
                                    <xsl:value-of select="regex-group(5)"/>
                                </xsl:when>
                                <xsl:when test="not($SILbookCode='previousBookCodeHere')">
                                    <xsl:value-of select="regex-group(5)"/>
                                </xsl:when>
                                <xsl:otherwise>
                                    <xsl:text>previousChapterNumberHere</xsl:text>
                                </xsl:otherwise>
                            </xsl:choose>
                        </xsl:when>

                        <!--      <xsl:when test="regex-group(1)=','">
                            <xsl:choose>
                                <xsl:when test="regex-group(6)='-'">
                                    <xsl:text>previousChapterNumberHere</xsl:text>
                                </xsl:when>
                                <xsl:otherwise>
                                    <xsl:value-of select="regex-group(5)"/>
                                </xsl:otherwise>
                            </xsl:choose>

                        </xsl:when>
                   -->
                        <xsl:when
                            test="regex-group(1)=',' and not(regex-group(5) and regex-group(6)='-' and regex-group(7))">
                            <!-- best guess is that this is a verse with no chapter number reference-->
                            <xsl:text>previousChapterNumberHere</xsl:text>
                        </xsl:when>
                        <xsl:when test="regex-group(6)='.'">
                            <!-- chapter  and verse reference-->
                            <xsl:value-of select="regex-group(5)"/>
                        </xsl:when>
                        <xsl:when test="regex-group(5) and regex-group(7)">
                            <!-- chapter  and verse reference-->
                            <xsl:value-of select="regex-group(5)"/>
                        </xsl:when>
                        <xsl:when test="regex-group(6)='' ">
                            <!-- best guess is that this is a chapter number -->
                            <xsl:value-of select="regex-group(5)"/>
                        </xsl:when>
                        <xsl:when test="regex-group(6)='—' and not(regex-group(1))">
                            <!-- best guess is that this is a chapter bridge with missing chapter number -->
                            <xsl:value-of select="regex-group(5)"/>
                        </xsl:when>
                        <xsl:when test="regex-group(6)='-' and not(regex-group(1))">
                            <!-- best guess is that this is a verse bridge with missing chapter number -->
                            <!-- Mat 6-7 -->
                            <xsl:value-of select="regex-group(5)"/>
                            <!--       <xsl:text>previousChapterNumberHere</xsl:text>-->
                        </xsl:when>
                        <xsl:when
                            test="regex-group(9)='-' or regex-group(9)='&mdash;' or regex-group(9)='&ndash;'">
                            <!-- extended reference with chapter or verse bridge -->
                            <xsl:value-of select="regex-group(5)"/>
                        </xsl:when>
                        <xsl:when
                            test="regex-group(9)=':' or regex-group(9)='.' or regex-group(9)=','">
                            <!-- extended reference with chapter verse separator -->
                            <xsl:value-of select="regex-group(5)"/>
                        </xsl:when>
                        <xsl:when test="$semicolonPreceedsReference">
                            <xsl:value-of select="regex-group(5)"/>
                        </xsl:when>
                        <xsl:when test="regex-group(5) and not(regex-group(7))">
                            <xsl:value-of select="regex-group(5)"/>
                            <!-- series in Pslams
                 -->
                        </xsl:when>
                        <xsl:when test="regex-group(6)=''">
                            <!-- best guess is that this is a chapter with no verse number -->
                            <xsl:value-of select="regex-group(5)"/>
                        </xsl:when>
                        <xsl:otherwise>
                            <xsl:value-of select="regex-group(5)"/>
                        </xsl:otherwise>
                    </xsl:choose>
                </xsl:variable>

                <xsl:variable name="verseNumber">
                    <xsl:choose>
                        <xsl:when test="$bookHasOnlyOneChapter=true()">
                            <xsl:value-of select="regex-group(5)"/>
                        </xsl:when>
                        <xsl:when test="regex-group(1)=','">
                            <xsl:choose>
                                <xsl:when test="regex-group(6)='-'">
                                    <xsl:value-of select="regex-group(5)"/>
                                </xsl:when>
                                <xsl:when
                                    test="regex-group(5) and not(regex-group(7)) and $SILbookCode='PSA'">
                                    <!-- series of chapters in Psalms -->
                                    <xsl:text>1</xsl:text>
                                </xsl:when>
                                <xsl:when test="regex-group(5) and not(regex-group(7))">
                                    <!-- not a series of chapters in Psalms -->
                                    <xsl:value-of select="regex-group(5)"/>
                                </xsl:when>
                                <xsl:otherwise>
                                    <xsl:value-of select="regex-group(7)"/>
                                </xsl:otherwise>
                            </xsl:choose>

                        </xsl:when>
                        <xsl:when test="regex-group(6)='—' and not(regex-group(1))">
                            <!-- best guess is that this is a chapter bridge with missing chapter number -->
                            <xsl:text>1</xsl:text>
                        </xsl:when>
                        <xsl:when test="regex-group(6)='-' and not(regex-group(1))">
                            <!-- best guess is that this is a verse bridge with missing chapter number -->
                            <xsl:text>1</xsl:text>
                        </xsl:when>
                        <xsl:when test="regex-group(5) and regex-group(7)">
                            <!-- chapter  and verse reference-->
                            <xsl:value-of select="regex-group(7)"/>
                        </xsl:when>
                        <xsl:when test="regex-group(1)=','">
                            <!-- best guess is that this is a verse with no chapter number reference-->
                            <xsl:value-of select="regex-group(5)"/>
                        </xsl:when>
                        <xsl:when test="regex-group(6)='' or regex-group(6)=','">
                            <!-- best guess is that this is a chapter  with no verse reference-->
                            <xsl:text>1</xsl:text>
                        </xsl:when>
                        <xsl:when
                            test="regex-group(9)=':' or regex-group(9)='.' or regex-group(9)=','">
                            <!-- extended reference with chapter verse separator -->
                            <xsl:value-of select="regex-group(7)"/>
                        </xsl:when>
                        <xsl:when test="regex-group(1) and not(regex-group(7))">
                            <xsl:text>1</xsl:text>
                        </xsl:when>
                        <!-- period is end punctuation -->
                        <xsl:when
                            test="regex-group(5) and regex-group(6)='.' and not(regex-group(7))">
                            <xsl:text>1</xsl:text>
                        </xsl:when>
                        <xsl:when test="regex-group(5) and not(regex-group(7))">
                            <xsl:value-of select="regex-group(5)"/>
                        </xsl:when>
                        <xsl:when test="regex-group(7)">
                            <xsl:value-of select="regex-group(7)"/>
                        </xsl:when>
                        <xsl:when test="regex-group(6)=''">
                            <!-- best guess is that this is a chapter with no verse number -->
                            <xsl:text>1</xsl:text>
                        </xsl:when>
                        <xsl:otherwise>
                            <xsl:text>1</xsl:text>
                        </xsl:otherwise>
                    </xsl:choose>
                </xsl:variable>

                <!--      <xsl:call-template name="semicolonPreceedsReference">
                    <xsl:with-param name="semicolonPreceedsReference"
                        select="$semicolonPreceedsReference"/>
                </xsl:call-template>
-->

                <xsl:value-of select="regex-group(1)"/>
                <xsl:value-of select="regex-group(2)"/>
                <xsl:element name="ref">
                    <!-- pos attribute start -->
                    <xsl:attribute name="tgt">
                        <xsl:value-of select="$SILbookCode"/>
                        <xsl:text>.</xsl:text>
                        <xsl:value-of select="$chapterNumber"/>
                        <xsl:text>.</xsl:text>
                        <xsl:value-of select="$verseNumber"/>

                        <!-- single chapter book -->
                        <!-- full book chapter verse found -->
                        <!-- (no book) chapter verse -->
                        <!-- (no chapter)-->
                        <!-- (no verse) -->
                        <!--  -->
                        <!--  -->
                        <!--  -->


                        <!--

                        <xsl:choose>
                        <xsl:when test="$bookHasOnlyOneChapter=true()">
                            </xsl:when>
                            <xsl:otherwise>
                                <xsl:choose>
                                    <xsl:when test="$bookNamePresent">
                            <! - - book name present - ->
                        </xsl:choose>

                    </xsl:attribute>
                    <! - - pos attribute end - ->
         -->

                    </xsl:attribute>
                    <!-- human reference start -->
                    <xsl:value-of select="regex-group(3)"/>
                    <xsl:value-of select="regex-group(4)"/>
                    <xsl:value-of select="regex-group(5)"/>
                    <xsl:value-of select="regex-group(6)"/>
                    <xsl:value-of select="regex-group(7)"/>
                    <xsl:value-of select="regex-group(8)"/>
                    <xsl:value-of select="regex-group(9)"/>
                    <xsl:value-of select="regex-group(10)"/>
                    <xsl:value-of select="regex-group(11)"/>
                    <xsl:value-of select="regex-group(12)"/>
                    <xsl:value-of select="regex-group(13)"/>
                    <xsl:value-of select="regex-group(14)"/>
                    <xsl:value-of select="regex-group(15)"/>
                    <xsl:value-of select="regex-group(16)"/>
                    <!-- human reference end -->
                </xsl:element>
            </xsl:matching-substring>
            <xsl:non-matching-substring>
                <xsl:value-of select="."/>
            </xsl:non-matching-substring>
        </xsl:analyze-string>
    </xsl:template>


    <xsl:template name="convertBookName2SILBook">
        <xsl:param name="bookName"> </xsl:param>
        <xsl:choose>
            <xsl:when test="$bookName=''">
                <xsl:value-of select="$bookName"/>

                <xsl:text>previousBookCodeHere</xsl:text>
            </xsl:when>


            <xsl:otherwise>
                <xsl:variable name="bookCode"
                    select="key('abbr2book',$bookName, $documentName)/@code"/>
                <xsl:value-of select="$bookCode"/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>


    <xsl:template name="bookNameNotPresent">
        <xsl:param name="chapterVerseSeparatorPresent" select="."/>
        <xsl:choose>
            <xsl:when test="$chapterVerseSeparatorPresent=true()">
                <!-- chapter verse separator present -->
                <xsl:call-template name="chapterVerseSeparatorPresent">
                    <xsl:with-param name="chapterNumber" select="regex-group(5)"/>
                    <xsl:with-param name="verseNumber" select="regex-group(7)"/>
                </xsl:call-template>
                <xsl:choose>
                    <xsl:when test="regex-group(7)">
                        <!-- verse number present -->
                        <xsl:value-of select="regex-group(5)"/>
                        <xsl:text>.</xsl:text>
                        <xsl:value-of select="regex-group(7)"/>
                    </xsl:when>
                    <xsl:otherwise>
                        <!-- verse number NOT present -->
                        <xsl:value-of select="regex-group(5)"/>
                        <xsl:text>.1</xsl:text>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
            <xsl:otherwise>
                <!-- chapter verse separator NOT present -->
                <xsl:text>previousChapterNumberHere</xsl:text>
                <xsl:text>.</xsl:text>
                <!-- verse number -->
                <xsl:value-of select="regex-group(5)"/>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <xsl:template name="chapterVerseSeparatorPresent">
        <xsl:param name="chapterNumber"/>
        <xsl:param name="verseNumber"/>
        <xsl:value-of select="$chapterNumber"/>
        <!-- verse number present -->
        <xsl:text>.</xsl:text>
        <xsl:value-of select="$verseNumber"/>
        <!--   <xsl:otherwise>
            < ! - - verse number NOT present - - >
            <xsl:text>.1</xsl:text>
        </xsl:otherwise>-->

    </xsl:template>

    <xsl:template name="bookNamePresent">
        <xsl:param name="bookCode"/>
        <xsl:param name="chapterNumber"/>
        <xsl:param name="verseNumber"/>



        <!-- chapter verse separator present -->
        <!--         <xsl:when test="$chapterVerseSeparatorPresent=true()">
            <xsl:call-template name="chapterVerseSeparatorPresent"></xsl:call-template></xsl:when>
        <xsl:when test="$chapterBridgePresent=true()">
            <xsl:value-of select="regex-group(5)"/>
            <xsl:text>.1</xsl:text>
        </xsl:when>
        <xsl:otherwise>
            <!- - chapter verse separator NOT present aned chapter bridge not presrent - ->
            <xsl:choose>
                <xsl:when test="$bookHasOnlyOneChapter=true()">
                    <xsl:text>1.</xsl:text>
                    <xsl:value-of select="regex-group(7)"/>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:value-of select="regex-group(5)"/>
                    <xsl:text>.1</xsl:text>
                </xsl:otherwise>
            </xsl:choose>
        </xsl:otherwise>
    </xsl:choose>
    <xsl:otherwise>
        <!- - book name NOT present - ->
        <xsl:call-template name="bookNameNotPresent">
            <xsl:with-param name="chapterVerseSeparatorPresent" select="$chapterVerseSeparatorPresent"/>
        </xsl:call-template> 
        
    </xsl:otherwise>
    -->
    </xsl:template>

    <xsl:template name="singleChapterBook">
        <xsl:param name="verseNumber" select="."/>
        <!-- chapter is always 1 by default -->
        <xsl:text>1.</xsl:text>
        <xsl:value-of select="regex-group(5)"/>

    </xsl:template>

</xsl:stylesheet>
