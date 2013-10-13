<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0">



    <!-- identity transform -->
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

    <xsl:template match="test">
        <xsl:variable name="testRefs" select="descendant-or-self::ref/@tgt"/>
        <xsl:variable name="outputRefs" select="following-sibling::output[1]/descendant-or-self::ref/@tgt"/>
        <xsl:choose>
            <xsl:when test="following-sibling::*[1]=following-sibling::output[1]">
                <xsl:choose>
                    <xsl:when test="count($testRefs)=0 and count($outputRefs)=0">
                        <xsl:text>pass No Refs</xsl:text>
                        <xsl:text>
         </xsl:text>
                        <xsl:value-of select="."/>
                        
                    
                    </xsl:when>        
                    <xsl:when test="count($testRefs)=count($outputRefs) and ($testRefs=$outputRefs)">
                        <xsl:text>pass </xsl:text>
                        <xsl:value-of select="$testRefs"/>
                        <xsl:text>
         </xsl:text>
                        <xsl:value-of select="$outputRefs"/>
                        <xsl:text>
         </xsl:text>
                        <xsl:value-of select="."/>

                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:text>Fail </xsl:text>
                        <xsl:value-of select="$testRefs"/>
                        <xsl:text>
         </xsl:text>
                        <xsl:value-of select="$outputRefs"/>
                        <xsl:text>
         </xsl:text>
                        <xsl:value-of select="."/>

                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
            <xsl:otherwise>
                <xsl:text>Please provide required output </xsl:text>
                <xsl:value-of select="."/>
                <xsl:text>
         </xsl:text>
                <xsl:value-of select="$outputRefs"/>
            </xsl:otherwise>

        </xsl:choose>


    </xsl:template>

    <xsl:template match="output"/>

</xsl:stylesheet>
