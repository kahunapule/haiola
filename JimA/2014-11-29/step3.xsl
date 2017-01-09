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

<!-- history -->
<!-- 2014-10-03 fixing error -->
<!-- 2013-09-26 add \r support -->
<!-- 2013-10-03 add ltr support -->
<!-- 2013-10-10 made more generic to fix <k> disappearing problem -->

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0">
    <xsl:output method="xml"/>
    <xsl:param name="bookNamesFile">../listOfBookNames.html</xsl:param>
    <xsl:param name="bookNamesXml">../BookNames.xml</xsl:param>
    <!-- I hard coded the params -->


    <xsl:variable name="documentName" select="document($bookNamesXml)"/>
    <xsl:variable name="allowedBooksAbbreviationShortLong"
        select="document($bookNamesFile)/html/body/p/substring(text(),1,string-length(text())-1)"/>

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

    <xsl:template match="output">
        <xsl:copy-of select="."/>
    </xsl:template>

    <xsl:template
        match="test|note|char[@style='xt']|char[@style='xdc']|para[@style='r']|x|f|ft|fe|th|thr|tr|tc|cs[@sfm='rq']|rq|p[starts-with(@sfm,'i')]|p[@sfm='pc']|p[@sfm='r']|ex|qot|qnt|xot_refs|xnt_refs|xdc_refs|xt|xtSee|xtSeeAlso|ef|esb">
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

            <!-- 2013-10-10 made more generic to fix <k> disappearing problem -->
            <!--
            <xsl:when test="child::note">
                < ! - - usx style only needed for sample file where /test/ is element above  - - >
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
            <xsl:when test="child::char[@style='xo']">
                < ! - - usx style - - >
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
            <xsl:when test="child::char[@style='xt']">
                < ! - - usx style - - >
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:call-template name="hyperlink">
                        <xsl:with-param name="string" select="."/>
                        <xsl:with-param name="bidiOn" select="ancestor-or-self::*/@bidi"/>
                    </xsl:call-template>
                </xsl:element>
            </xsl:when>
            <xsl:when test="child::char[@style='xdc']">
                < ! - - usx style - - >
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
            <xsl:when test="child::x">
                < ! - - usfx style - - >
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
            < ! - - xo shows where cross-reference is located - - >
            <xsl:when test="child::xo">
                < ! - - usfx style - ->
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
            <xsl:when test="child::xt">
                < ! - - usfx style - - >
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
            < ! - - footnote reference shows where footnote is located - - >
            <xsl:when test="child::fr">
                < ! - - usfx style - ->
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
            <xsl:when test="child::ft">
                < ! - - usfx style - - >
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
            <xsl:when test="child::k">
                < ! - - usfx style - - >
                <xsl:element name="{local-name()}">
                    <xsl:copy-of select="@*"/>
                    <xsl:apply-templates/>
                </xsl:element>
            </xsl:when>
            </xsl:when>
            -->
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
        <!--    regex="([;,])?(\s)?({$allowedBooksAbbreviationShortLong})?\.?([&nbsp;\s])?([0-9]+)([\.:,&mdash;&nbsp;-])?([0-9]+)?([a-z]*)([\.:,&mdash;&nbsp;-])?([0-9]+)?([a-z]*)?">-->
        <!--         1 initial comma or ;/ 2 space/ 3 book name or abbrevation (optional . removed latter)/  4 space /  5 chapter or verse number / 6 punctuation : and : are different/ 7 verse number/ 8 abff / 9 verse number / 10 abff /                                                                                                                  -->
        <!--regex="([,;؛])*(\s)?({$allowedBooksAbbreviationShortLong})?\.?([&nbsp;\s])?(&lrm;?[0-9]+)([\.::,&mdash;&nbsp;-])?([0-9]+)?([a-z]*)([\.::&mdash;&nbsp;-])?([0-9]+)?([a-z]*)?(&rlm;?[\.::&mdash;&nbsp;-])?(&lrm;?[0-9]+)?([a-z]*)">-->
        <!-- <xsl:analyze-string select="$string"
            regex="(.)+">
        (.)?(&rlm;)?({$allowedBooksAbbreviationShortLong})(.*)(:)(.?)(.)*  
        -->
        <xsl:analyze-string select="$string"
            regex="(&rlm;?[,;&arabicSemicolon;&arabicComma;])*(\s)?({$allowedBooksAbbreviationShortLong})?\.?(&rlm;?[&nbsp;\s])?([0-9]+)(&rlm;?[\.:,&arabicColon;&arabicComma;&mdash;&nbsp;-])?([0-9]+)?([a-z]*)(&rlm;?[\.:&arabicColon;&mdash;&nbsp;-])?([0-9]+)?([a-z]*)?(&rlm;?[\.:&arabicColon;&mdash;&nbsp;-])?([0-9]+)?([a-z]*)">


            <xsl:matching-substring>
                <!-- start debugging code 
                set test=false() to turn off debugging
                set test=true() to turn on debugging
                
                -->
                <xsl:choose>
                    <xsl:when test="false()">
                        <!-- set test="true()" to see debugging output
                            set test="false()" for production
                            -->

                        <xsl:text>

                </xsl:text>
                        <xsl:value-of select="$string"/>
                        <xsl:text>
</xsl:text>
                        <!--                        <xsl:text>bidi=</xsl:text>
                        <xsl:value-of select="$bidi"/>
                        <xsl:text>bidiOn=</xsl:text>
                        <xsl:value-of select="$bidiOn"/>
   -->
                        <xsl:text>&lrm;</xsl:text>
                        <xsl:text>/</xsl:text>
                        <xsl:text>1=</xsl:text>
                        <xsl:value-of select="regex-group(1)"/>
                        <xsl:text>&lrm;</xsl:text>
                        <xsl:text>/</xsl:text>
                        <xsl:text>2=</xsl:text>
                        <xsl:value-of select="regex-group(2)"/>
                        <xsl:text>&lrm;</xsl:text>
                        <xsl:text>/</xsl:text>
                        <xsl:text>3=</xsl:text>
                        <xsl:value-of select="regex-group(3)"/>
                        <xsl:text>&lrm;</xsl:text>
                        <xsl:text>/</xsl:text>
                        <xsl:text>4=</xsl:text>
                        <xsl:value-of select="regex-group(4)"/>
                        <xsl:text>&lrm;</xsl:text>
                        <xsl:text>/</xsl:text>
                        <xsl:text>5=</xsl:text>
                        <xsl:value-of select="regex-group(5)"/>
                        <xsl:text>&lrm;</xsl:text>
                        <xsl:text>/</xsl:text>
                        <xsl:text>6=</xsl:text>
                        <xsl:value-of select="regex-group(6)"/>
                        <xsl:text>&lrm;</xsl:text>
                        <xsl:text>/</xsl:text>
                        <xsl:text>7=</xsl:text>
                        <xsl:value-of select="regex-group(7)"/>
                        <xsl:text>&lrm;</xsl:text>
                        <xsl:text>/</xsl:text>
                        <xsl:text>8=</xsl:text>
                        <xsl:value-of select="regex-group(8)"/>
                        <xsl:text>&lrm;</xsl:text>
                        <xsl:text>/</xsl:text>
                        <xsl:text>9=</xsl:text>
                        <xsl:value-of select="regex-group(9)"/>
                        <xsl:text>&lrm;</xsl:text>
                        <xsl:text>/</xsl:text>
                        <xsl:text>10=</xsl:text>
                        <xsl:value-of select="regex-group(10)"/>
                        <xsl:text>/</xsl:text>
                        <xsl:text>11=</xsl:text>
                        <xsl:value-of select="regex-group(11)"/>
                        <xsl:text>/</xsl:text>
                        <xsl:text>
</xsl:text>

                    </xsl:when>
                    <xsl:otherwise>
                        <!-- debugging code skipped -->
                    </xsl:otherwise>
                </xsl:choose>
                <!-- end debugging code -->
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
                        <xsl:when test="$bookHasOnlyOneChapter=true() ">
                            <xsl:text>1</xsl:text>
                        </xsl:when>
                        <xsl:when test="regex-group(1)=',' or regex-group(1)='&arabicComma;'">
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
                        <xsl:when
                            test="regex-group(1)='&arabicComma;' and not(regex-group(5) and regex-group(6)='-' and regex-group(7))">
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
                    <!-- at one time I thougtht I would need different methods for verseNumber if RTL but that proved false -->
                    <xsl:call-template name="verseNumber">
                        <xsl:with-param name="bookHasOnlyOneChapter" select="$bookHasOnlyOneChapter"/>
                        <xsl:with-param name="SILbookCode" select="$SILbookCode"/>
                    </xsl:call-template>
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
                    <!-- human reference end -->
                </xsl:element>
            </xsl:matching-substring>
            <xsl:non-matching-substring>
                <xsl:value-of select="."/>
            </xsl:non-matching-substring>
        </xsl:analyze-string>
    </xsl:template>

    <xsl:template name="verseNumber">
        <xsl:param name="bookHasOnlyOneChapter"/>
        <xsl:param name="SILbookCode"/>
        <xsl:param name="verseNumberPresent" select="boolean(regex-group(7))"/>
        <xsl:param name="chapterNumber" select="regex-group(5)"/>
        <xsl:param name="chapterBridgePresent"
            select="boolean(regex-group(6)='&mdash;') or boolean(regex-group(6)='&nbsp;') or boolean(regex-group(6)='-') "/>
        <xsl:param name="chapterVerseSeparatorPresent"
            select="regex-group(6)='.' or regex-group(6)=',' or regex-group(6)=':'"/>

        <xsl:choose>
            <!-- single chapter book with chapter verse separator -->
            <xsl:when test="$bookHasOnlyOneChapter=true() and $chapterVerseSeparatorPresent and $chapterNumber='1'">
                <xsl:value-of select="regex-group(7)"/>
            </xsl:when>
            <xsl:when test="$bookHasOnlyOneChapter=true() and $chapterVerseSeparatorPresent">
                <xsl:value-of select="regex-group(5)"/>
            </xsl:when>
            <xsl:when test="$bookHasOnlyOneChapter=true() and $chapterBridgePresent">
                <xsl:value-of select="regex-group(5)"/>
            </xsl:when>
            <xsl:when test="$bookHasOnlyOneChapter=true() and $verseNumberPresent=false()">
                <xsl:value-of select="regex-group(5)"/>
            </xsl:when>
            <xsl:when test="$bookHasOnlyOneChapter=true() and $verseNumberPresent=true()">
                <xsl:value-of select="regex-group(7)"/>
            </xsl:when>
            <xsl:when test="regex-group(1)=',' or regex-group(1)='&arabicComma;'">
                <xsl:choose>
                    <xsl:when test="regex-group(6)='-'">
                        <xsl:value-of select="regex-group(5)"/>
                    </xsl:when>
                    <xsl:when test="regex-group(5) and not(regex-group(7)) and $SILbookCode='PSA'">
                        <!-- series of chapters in Psalms -->
                        <xsl:text>1</xsl:text>
                    </xsl:when>
                    <xsl:when test="regex-group(5) and not(regex-group(7))">
                        <!-- not a series of chapters in Psalms, this is a series of verses -->
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
            <xsl:when test="regex-group(9)=':' or regex-group(9)='.' or regex-group(9)=','">
                <!-- extended reference with chapter verse separator -->
                <xsl:value-of select="regex-group(7)"/>
            </xsl:when>
            <xsl:when test="regex-group(1) and not(regex-group(7))">
                <xsl:text>1</xsl:text>
            </xsl:when>
            <!-- period is end punctuation -->
            <xsl:when test="regex-group(5) and regex-group(6)='.' and not(regex-group(7))">
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
    </xsl:template>

    <xsl:template name="verseNumberLTR">
        <xsl:param name="bookHasOnlyOneChapter"/>
        <xsl:param name="SILbookCode"/>
        <xsl:choose>
            <xsl:when test="$bookHasOnlyOneChapter=true()">
                <xsl:value-of select="regex-group(5)"/>
            </xsl:when>
            <xsl:when test="regex-group(1)=','">
                <xsl:choose>
                    <xsl:when test="regex-group(6)='-'">
                        <xsl:value-of select="regex-group(5)"/>
                    </xsl:when>
                    <xsl:when test="regex-group(5) and not(regex-group(7)) and $SILbookCode='PSA'">
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
            <xsl:when test="regex-group(9)=':' or regex-group(9)='.' or regex-group(9)=','">
                <!-- extended reference with chapter verse separator -->
                <xsl:value-of select="regex-group(7)"/>
            </xsl:when>
            <xsl:when test="regex-group(1) and not(regex-group(7))">
                <xsl:text>1</xsl:text>
            </xsl:when>
            <!-- period is end punctuation -->
            <xsl:when test="regex-group(5) and regex-group(6)='.' and not(regex-group(7))">
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

    </xsl:template>
    <xsl:template name="verseNumberRTL">
        <xsl:param name="bookHasOnlyOneChapter"/>
        <xsl:param name="SILbookCode"/>
        <xsl:choose>
            <xsl:when test="$bookHasOnlyOneChapter=true()">
                <xsl:value-of select="regex-group(5)"/>
            </xsl:when>
            <xsl:when test="regex-group(1)=','">
                <xsl:choose>
                    <xsl:when test="regex-group(6)='-'">
                        <xsl:value-of select="regex-group(5)"/>
                    </xsl:when>
                    <xsl:when test="regex-group(5) and not(regex-group(7)) and $SILbookCode='PSA'">
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
            <xsl:when test="regex-group(9)=':' or regex-group(9)='.' or regex-group(9)=','">
                <!-- extended reference with chapter verse separator -->
                <xsl:value-of select="regex-group(7)"/>
            </xsl:when>
            <xsl:when test="regex-group(1) and not(regex-group(7))">
                <xsl:text>1</xsl:text>
            </xsl:when>
            <!-- period is end punctuation -->
            <xsl:when test="regex-group(5) and regex-group(6)='.' and not(regex-group(7))">
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

        <xsl:text>.1</xsl:text>

    </xsl:template>

    <!--
    <xsl:template name="singleChapterBook">
        <xsl:param name="verseNumber" select="."/>
        < ! - - chapter is always 1 by default - - >
        <xsl:text>xxxxxxxx</xsl:text>
        <xsl:text>1.</xsl:text>
        <xsl:value-of select="regex-group(5)"/>
<xsl:choose>
    <xsl:when test="regex-group(5) and regex-group(7)">
        <xsl:value-of select="regex-group(3)"/>
        <xsl:text>.1.</xsl:text>
        <xsl:value-of select="regex-group(7)"/>
    </xsl:when>
<xsl:otherwise></xsl:otherwise>
</xsl:choose>
    </xsl:template>
-->
</xsl:stylesheet>
