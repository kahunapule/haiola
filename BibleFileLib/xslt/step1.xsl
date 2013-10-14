<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE stylesheet [
<!ENTITY cr "&#xD;&#xA;">
<!ENTITY tab "&#9;">
<!ENTITY nbsp "&#160;">
]>


<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0">
    <xsl:template match="BookNames">
        <html>
            <body>
                <p>
                    <xsl:for-each select="book">
                        <xsl:choose>
                            <xsl:when test="@abbr">
                                <xsl:choose>
                                    <xsl:when test="@abbr=''">
                                        <!-- skip -->
                                    </xsl:when>
                                    <xsl:otherwise>
                                        <xsl:value-of select="@abbr"/>
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
                                        <xsl:value-of select="@short"/>
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
                                        <xsl:value-of select="@alt"/>
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
                                        <xsl:value-of select="substring-before(@long,',')"/>
                                        <xsl:text>|</xsl:text>
                                    </xsl:when>
                                    <xsl:otherwise>
                                        <xsl:value-of select="@long"/>
                                        <xsl:text>|</xsl:text>
                                    </xsl:otherwise>
                                </xsl:choose>
                            </xsl:when>
                        </xsl:choose>
                    </xsl:for-each>
                </p>
            </body>
        </html>
    </xsl:template>



</xsl:stylesheet>
