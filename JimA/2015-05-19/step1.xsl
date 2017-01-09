<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE stylesheet [
<!ENTITY cr "&#xD;&#xA;">
<!ENTITY tab "&#9;">
<!ENTITY nbsp "&#160;">
]>

<!-- history -->
<!-- 2-13-09-27 Esther (Greek) moves all of the matching groups up a level so escape parens -->

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0">
    <xsl:template match="BookNames">
        <html>
            <body>
                <p>
                    <xsl:call-template name="booksStartingWithNumber"/>
                    <xsl:call-template name="allBooks"/>
                    
                </p>
            </body>
        </html>
    </xsl:template>

<xsl:template name="allBooks">
    <xsl:for-each select="book">
        <xsl:choose>
            <xsl:when test="@abbr">
                <xsl:choose>
                    <xsl:when test="@abbr=''">
                        <!-- skip -->
                    </xsl:when>
                    <xsl:otherwise>
                        <!-- assume no parens in abbreviation -->
                        <xsl:variable name="removePeriod"
                            select="replace(@abbr,'\.','')"/>
                        <xsl:variable name="addPeriod"
                            select="concat($removePeriod,'\.')"/>
                        <xsl:value-of select="$removePeriod"/>
                        <xsl:text>|</xsl:text>
                        <xsl:value-of select="$addPeriod"/>
                        <xsl:text>|</xsl:text>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
            
        </xsl:choose>
        <xsl:choose>
            <xsl:when test="@short">
                <xsl:choose>
                    <xsl:when test="@short=''">
                        <!-- skip -->
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:variable name="replacedOpenParens"
                            select="replace(@short,'\(','\\(')"/>
                        <xsl:variable name="replacedPeriod"
                            select="replace($replacedOpenParens,'\.','\\.')"/>
                        <xsl:value-of select="replace($replacedPeriod,'\)','\\)')"/>
                        <xsl:text>|</xsl:text>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
        </xsl:choose>
        <xsl:choose>
            <xsl:when test="@alt">
                <xsl:choose>
                    <xsl:when test="@alt=''">
                        <!-- skip -->
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:variable name="replacedOpenParens"
                            select="replace(@alt,'\(','\\(')"/>
                        <xsl:variable name="replacedPeriod"
                            select="replace($replacedOpenParens,'\.','\\.')"/>
                        <xsl:value-of select="replace($replacedPeriod,'\)','\\)')"/>
                        <xsl:text>|</xsl:text>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
        </xsl:choose>
        <xsl:choose>
            <xsl:when test="@long">
                <xsl:choose>
                    <xsl:when test="@long=''">
                        <!-- skip -->
                    </xsl:when>
                    <xsl:when test="contains(@long,',')">
                        <xsl:variable name="replacedOpenParens"
                            select="replace(@long,'\(','\\(')"/>
                        <xsl:variable name="replacedBothParens"
                            select="replace($replacedOpenParens,'\)','\\)')"/>
                        <xsl:value-of
                            select="substring-before($replacedBothParens,',')"/>
                        <!-- may need to add repace period here too -->
                        <xsl:text>|</xsl:text>
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:variable name="replacedOpenParens"
                            select="replace(@long,'\(','\\(')"/>
                        <xsl:variable name="replacedPeriod"
                            select="replace($replacedOpenParens,'\.','\\.')"/>
                        <xsl:value-of select="replace($replacedPeriod,'\)','\\)')"/>
                        <xsl:text>|</xsl:text>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
        </xsl:choose>
    </xsl:for-each>
    
</xsl:template>

<xsl:template name="booksStartingWithNumber">
    <xsl:for-each select="book[substring(@abbr,1,1)='1']|book[substring(@abbr,1,1)='2']|book[substring(@abbr,1,1)='3']|book[substring(@abbr,1,1)='4']">
        <xsl:choose>
            <xsl:when test="@abbr">
                <xsl:choose>
                    <xsl:when test="@abbr=''">
                        <!-- skip -->
                    </xsl:when>
                    <xsl:otherwise>
                        <!-- assume no parens in abbreviation -->
                        <xsl:variable name="removePeriod"
                            select="replace(@abbr,'\.','')"/>
                        <xsl:variable name="addPeriod"
                            select="concat($removePeriod,'\.')"/>
                        <xsl:value-of select="$removePeriod"/>
                        <xsl:text>|</xsl:text>
                        <xsl:value-of select="$addPeriod"/>
                        <xsl:text>|</xsl:text>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
            
        </xsl:choose>
        <xsl:choose>
            <xsl:when test="@short">
                <xsl:choose>
                    <xsl:when test="@short=''">
                        <!-- skip -->
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:variable name="replacedOpenParens"
                            select="replace(@short,'\(','\\(')"/>
                        <xsl:variable name="replacedPeriod"
                            select="replace($replacedOpenParens,'\.','\\.')"/>
                        <xsl:value-of select="replace($replacedPeriod,'\)','\\)')"/>
                        <xsl:text>|</xsl:text>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
        </xsl:choose>
        <xsl:choose>
            <xsl:when test="@alt">
                <xsl:choose>
                    <xsl:when test="@alt=''">
                        <!-- skip -->
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:variable name="replacedOpenParens"
                            select="replace(@alt,'\(','\\(')"/>
                        <xsl:variable name="replacedPeriod"
                            select="replace($replacedOpenParens,'\.','\\.')"/>
                        <xsl:value-of select="replace($replacedPeriod,'\)','\\)')"/>
                        <xsl:text>|</xsl:text>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
        </xsl:choose>
        <xsl:choose>
            <xsl:when test="@long">
                <xsl:choose>
                    <xsl:when test="@long=''">
                        <!-- skip -->
                    </xsl:when>
                    <xsl:when test="contains(@long,',')">
                        <xsl:variable name="replacedOpenParens"
                            select="replace(@long,'\(','\\(')"/>
                        <xsl:variable name="replacedBothParens"
                            select="replace($replacedOpenParens,'\)','\\)')"/>
                        <xsl:value-of
                            select="substring-before($replacedBothParens,',')"/>
                        <!-- may need to add repace period here too -->
                        <xsl:text>|</xsl:text>
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:variable name="replacedOpenParens"
                            select="replace(@long,'\(','\\(')"/>
                        <xsl:variable name="replacedPeriod"
                            select="replace($replacedOpenParens,'\.','\\.')"/>
                        <xsl:value-of select="replace($replacedPeriod,'\)','\\)')"/>
                        <xsl:text>|</xsl:text>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
        </xsl:choose>
    </xsl:for-each>
    
</xsl:template>

</xsl:stylesheet>
