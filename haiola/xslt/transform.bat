@echo off

rem Jim Albright

rem 2013-09-03

rem start with string containing references

rem end with each found reference in standard form

 

 

echo . markup text so each canonical reference is identified for computer to understand 

 

if *%2*==** goto :usage

if *%1*==*usx* set %usx%=true

if *%1*==** set %usx%=false

 

 

:start

echo .

if exist %2\source\BookNames.xml goto :sourceUSX

echo . BookNames.xml not found in %2\source

goto badEnding

 

:sourceUSX

if exist %2\source\BookNames.xml goto :step1

echo .%1 not found in %2\source\%1

goto badEnding

echo .

echo .

 

:step1

echo ------------------------------------------------

echo . step 1 - create list of possible references 

echo ------------------------------------------------

echo . copy booknames.xml for processing

copy %2\source\BookNames.xml BookNames.xml

echo . output: convert\listOfBookNames.html

echo . input:  source\BookNames.xml

echo . xsl:    XSLT\step1.xsl

call "C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:listOfBookNames.html BookNames.xml XSLT\step1.xsl

echo ------------------------------------------------

copy listOfBookNames.html %2\convert\listOfBookNames.html

 

if exist listOfBookNames.html goto step2

goto badEnding

 

:step2

echo . step 1 completed

 

echo ------------------------------------------------

echo . step 2 - create list of possible references starting with digit 

echo ------------------------------------------------

echo . output: convert\listOfBookNamesStartingWithDigit.html

echo . input:  source\BookNames.xml

echo . xsl:    XSLT\step2.xsl

call "C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:listOfBookNamesStartingWithDigit.html BookNames.xml XSLT\step2.xsl

echo ------------------------------------------------

copy listOfBookNamesStartingWithDigit.html %2\convert\listOfBookNamesStartingWithDigit.html

 

if exist listOfBookNamesStartingWithDigit.html goto step3

goto badEnding

 

:step3

echo . step 2 completed

echo ------------------------------------------------

echo . step 3 - mark fully identified canonical references 

echo ------------------------------------------------

echo . usx:   %usx%

echo . folder: %2

echo . output: %2\convert\%1.step3.xml

echo . input:  %2\source\%1

echo . xsl:    XSLT\step3

if *%usx%*==*true* goto usx3

"C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:%2\convert\%1.step3.xml c:\d\Kahuna\%2\source\%1 XSLT\step3.xsl

goto end3

:usx3

for /f %%f in ('dir /b c:\d\Kahuna\%2\source\*.usx') do call "C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:%2\convert\%%~nf.333 c:\d\Kahuna\%2\source\%%f XSLT\step3.xsl bidi=%bidi%

 

 

echo ------------------------------------------------

 

:end3

if exist %2\convert\%1.step3.xml goto step4

if exist %2\convert\*.333 goto step4

goto badEnding

 

:step4

echo . step 3 completed

echo ------------------------------------------------

echo . step 4 - correct error with (2) parens

echo ------------------------------------------------

echo . output: %2\convert\%1.step4.xml

echo . input:  %2\convert\%1.step3.xml

echo . xsl:    XSLT\step4

 

 

if *%usx%*==*true* goto usx4

call "C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:%2\convert\%1.step4.xml %2\convert\%1.step3.xml XSLT\step4.xsl

goto end4

:usx4

for /f %%f in ('dir /b c:\d\Kahuna\%2\convert\*.333') do call "C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:%2\convert\%%~nf.444 c:\d\Kahuna\%2\convert\%%f XSLT\step4.xsl

 

echo ------------------------------------------------

 

:end4

if exist %2\convert\%1.step4.xml goto step5

if exist %2\convert\*.444 goto :step5

goto badEnding

 

:step5

echo . step 4 completed

echo ------------------------------------------------

echo . step 5 - add missing book abbreviation to canonical reference

echo.           add missing chapter number    to canonical reference 

echo ------------------------------------------------

echo . output: %2\convert\%1.step5.xml

echo . input:  %2\convert\%1.step4.xml

echo . xsl:    XSLT\step5

 

 

if *%usx%*==*true* goto usx5

call "C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:%2\convert\%1.step5.xml %2\convert\%1.step4.xml XSLT\step5.xsl

goto end5

:usx5

for /f %%f in ('dir /b c:\d\Kahuna\%2\convert\*.444') do call "C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:%2\convert\%%~nf.usx c:\d\Kahuna\%2\convert\%%f XSLT\step5.xsl

 

echo ------------------------------------------------

 

:end5

echo . step 5 completed

echo ------------------------------------------------

if exist %2\convert\%1.step5.xml  goto step6

if exist %2\convert\*.555 goto :step6

goto end

 

:step6

if *%2*==*sample* goto :validate

goto :end

:validate

echo ------------------------------------------------

echo . step 6 - validate sample

echo ------------------------------------------------

echo . output: %2\convert\sampleRefs.validate.usx

echo . input:  %2\convert\sampleRefs.usx

echo . xsl:    XSLT\validate.xsl

 

call "C:\Program Files\Saxonica\SaxonHE9.4N\bin\transform" -o:%2\convert\sampleRefs.validate.usx %2\convert\sampleRefs.usx XSLT\validate.xsl

 

echo ------------------------------------------------

 

if exist %2\convert\*.usx goto :end

 

goto badEnding

 

 

 

goto end

 

 

:badEnding

echo . error -- missing file

echo ------------------------------------------------

goto :end

 

:usage

echo ------------------------------------------------

echo . Usage:

echo . You must give a filename and folder

echo .

echo . example

echo identifyReferences.bat eng-web_usfx.xml eng-web

echo .

echo or

echo .

echo . If you use usx instead of a filename,

echo . all files ending with usx in the folder will be transformed

echo identifyReferences.bat usx ubs-gnb

echo .

echo . input file is assumed to be in folder/source/filename

echo or

echo . input files are assumed to be in folder/source/*.usx

echo .

echo . output is found in folder/convert

echo ------------------------------------------------

 

:end

echo ------------------------------------------------

echo . transformations ended

echo ------------------------------------------------

 