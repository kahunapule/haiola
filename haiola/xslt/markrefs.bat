del listOfBookNames.html listOfBookNamesStartingWithDigit.html temp3.xml temp4.xml usfxout.xml
java -jar saxon9he.jar -o:listOfBookNames.html -s:BookNames.xml -xsl:step1.xsl
java -jar saxon9he.jar -o:listOfBookNamesStartingWithDigit.html -s:BookNames.xml -xsl:step2.xsl
java -jar saxon9he.jar -o:temp3.xml -s:usfx.xml -xsl:step3.xsl
java -jar saxon9he.jar -o:temp4.xml -s:temp3.xml -xsl:step4.xsl
java -jar saxon9he.jar -o:usfxout.xml -s:temp4.xml -xsl:step5.xsl
