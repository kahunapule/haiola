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
                            <!-- book code starts with 1, (2 and 3  are repeats)-->
                            <xsl:when test="substring(@abbr,1,1)='1'">
                                <xsl:choose>
                                    <xsl:when test="@abbr">
                                        <xsl:choose>
                                            <xsl:when test="substring(@abbr,2,1)=' '">
                                                <!-- skip -->
                                                <xsl:text>,</xsl:text>
                                                <xsl:value-of select="substring-after(@abbr,' ')"/>
                                            </xsl:when>
                                            <xsl:otherwise>
                                                <!-- skip -->
                                            </xsl:otherwise>
                                        </xsl:choose>
                                    </xsl:when>
                                </xsl:choose>
                                <xsl:choose>
                                    <xsl:when test="@short">
                                        <xsl:choose>
                                            <xsl:when test="substring(@short,2,1)=' '">
                                                <!-- skip -->
                                                <xsl:text>,</xsl:text>
                                                <xsl:value-of select="substring-after(@short,' ')"/>
                                            </xsl:when>
                                            <xsl:otherwise>
                                                <!-- skip -->
                                            </xsl:otherwise>
                                        </xsl:choose>
                                    </xsl:when>
                                </xsl:choose>
                                <xsl:choose>
                                    <xsl:when test="@alt">
                                        <xsl:choose>
                                            <xsl:when test="substring(@alt,2,1)=' '">
                                                <!-- skip -->
                                                <xsl:text>,</xsl:text>
                                                <xsl:value-of select="substring-after(@alt,' ')"/>
                                            </xsl:when>
                                            <xsl:otherwise>
                                                <!-- skip -->
                                            </xsl:otherwise>
                                        </xsl:choose>
                                    </xsl:when>
                                </xsl:choose>
                                <xsl:choose>
                                    <xsl:when test="@long">
                                        <xsl:choose>
                                            <xsl:when test="substring(@long,2,1)=' '">
                                                <!-- skip -->
                                                <xsl:text>,</xsl:text>
                                                <xsl:value-of select="substring-after(@long,' ')"/>
                                            </xsl:when>
                                            <xsl:otherwise>
                                                <!-- skip -->
                                            </xsl:otherwise>
                                        </xsl:choose>
                                    </xsl:when>
                                </xsl:choose>
                            </xsl:when>
                            <xsl:otherwise>
                                <!-- skip -->
                            </xsl:otherwise>
                        </xsl:choose>
                    </xsl:for-each>
                </p>
            </body>
        </html>
    </xsl:template>



</xsl:stylesheet>
