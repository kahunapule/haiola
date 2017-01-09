@echo off
call "C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:listOfBookNames.html BookNames.xml XSLT\step1.xsl
call "C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:listOfBookNamesStartingWithDigit.html BookNames.xml XSLT\step2.xsl
call "C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:usfx/\convert\usfx.xml.step3.xml c:\d\Kahuna\usfx/\source\usfx.xml XSLT\step3.xsl pathToFiles="C:\d\kahuna\" bookNamesFile="listOfBookNames.html"  bookNamesXml="BookNames.xml"
call "C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:usfx/\convert\usfx.xml.step4.xml usfx/\convert\usfx.xml.step3.xml XSLT\step4.xsl
call "C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:usfx/\convert\usfx.xml.step5.xml usfx/\convert\usfx.xml.step4.xml XSLT\step5.xsl
call "C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:usfx/\convert\usfx.xml.step6.xml usfx/\convert\usfx.xml.step5.xml XSLT\step6.xsl
