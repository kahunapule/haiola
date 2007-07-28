<xsl:stylesheet version="1.0"
                xmlns:osis="http://www.bibletechnologies.net/2003/OSIS/namespace"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

	<xsl:output method="html" encoding="UTF-8"/>


	<!-- XSLT stylesheet for Bible text in OSIS format (output to HTML, with display.css file) -->
	<!-- Dave van Grootheest, Netherlands Bible Society -->
	<!-- last updated: 2007-07-12 -->


	<xsl:template match="/">
		<div class="BookChapIndex">
			<xsl:apply-templates select="//osis:title[@short]"/>
			<p class="IndexChapterList">
				<xsl:apply-templates select="//osis:chapter"/>
			</p>
		</div>

	</xsl:template>

	<xsl:template match="osis:title[@short]">

		<p class="IndexBookName">
			<a target="main" href="$$filename$$"><xsl:value-of select="@short"/></a>
		</p>

	</xsl:template>

	<xsl:template match="osis:chapter">

		<a target="main">
			<xsl:attribute name="href">$$filename$$#C<xsl:value-of select="@n"/></xsl:attribute>
			<xsl:value-of select="@n"/>
		</a>
		<xsl:text> </xsl:text>

	</xsl:template>
</xsl:stylesheet>