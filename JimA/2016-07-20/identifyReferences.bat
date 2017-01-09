@echo off
rem Jim Albright
rem 2013-09-03
rem 2014-10-14 corrected /Oba 1.14,/  error and also handle Oba 5,7 correctly
rem start with string containing references
rem end with each found reference in standard form
rem 2014-11-29 step 0 added to allow for period present or absent in text
rem 2014-11-29 needed to move /1 John/ before /John/ when matching (step 1)
rem 2014-12-19 Mak. 7.28; 2 Kor. 4.6  generated a malformed reference
rem may need to update Saxon if it has been awhile since processing files
rem 2016-07014 699 processed so far  25 failures to fix now
rem fixed a single chapter book error
rem fix error when () are included in book name
echo .
echo .
echo .
echo .
echo .
echo .
echo .
echo .
echo .
echo .
echo .
echo .
echo .
echo .
echo .
echo .
echo .
echo . markup text so each canonical reference is identified 
echo . for computer to understand  
echo .
if *%2*==** goto :usage
if *%1*==*usx* set %usx%=true
if *%1*==** set %usx%=false

:start
echo %time% > %2\convert\%1.timer.txt 
echo .

if exist %2\source\BookNames.xml goto :sourceUSX
echo . BookNames.xml not found in %2\source
goto badEnding

:sourceUSX
echo . Step A
:stepA
echo ------------------------------------------------
echo . step A - xxxxxxx (yyy) for a name should be xxxxxxx vertical bar yyy   
echo . parens mess with the regex
echo ------------------------------------------------
echo . output: %2\convert\BookNamesA.html
echo . input:  %2\source\BookNames.xml
echo . xsl:    XSLT\stepA.xsl
call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\BookNamesA.xml %2\source\BookNames.xml XSLT\stepA.xsl
echo ------------------------------------------------
if exist %2\convert\BookNamesA.xml goto step0
goto badEnding

:step0
echo ------------------------------------------------
echo . step 0 - create abbreviation with period and without period  
echo ------------------------------------------------
echo . output: %2\convert\BookNamesWithPeriodAndWithout.html
echo . input:  %2\source\BookNames.xml
echo . xsl:    XSLT\step0.xsl
call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\BookNamesWithPeriodAndWithout.html %2\convert\BookNamesA.xml XSLT\step0.xsl
echo ------------------------------------------------

if exist %2\convert\BookNamesWithPeriodAndWithout.html goto step1
goto badEnding


:step1
echo ------------------------------------------------
echo . step 1 - create list of possible references  
echo ------------------------------------------------
echo . output: %2\convert\listOfBookNames.html
echo . input:  %2\convert\BookNamesWithPeriodAndWithout.html
echo . xsl:    XSLT\step1.xsl
call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\listOfBookNames.html %2\convert\BookNamesWithPeriodAndWithout.html XSLT\step1.xsl
echo ------------------------------------------------

if exist %2\convert\listOfBookNames.html goto step2
goto badEnding

:step2
echo . step 1 completed

echo ------------------------------------------------
echo . step 2 - create list of possible references starting with digit  
echo ------------------------------------------------
echo . output: convert\listOfBookNamesStartingWithDigit.html
echo . input:  source\BookNames.xml
echo . xsl:    XSLT\step2.xsl
call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\listOfBookNamesStartingWithDigit.html %2\convert\BookNamesA.xml XSLT\step2.xsl
echo ------------------------------------------------
echo . step 2 completed

if exist %2\convert\listOfBookNamesStartingWithDigit.html goto step3
goto badEnding

:step3
echo . copy files to a known location so xslt can reference these files
copy %2\convert\BookNamesWithPeriodAndWithout.html BookNamesWithPeriodAndWithout.html
copy %2\convert\listOfBookNames.html listOfBookNames.html
copy %2\convert\listOfBookNamesStartingWithDigit.html listOfBookNamesStartingWithDigit.html
copy %2\convert\BookNamesA.xml BookNames.xml


if *%3*==*Persian* goto :Persian3fixHyphen
echo . regular parsing
echo ------------------------------------------------
echo . step 3 - mark fully identified canonical references  
echo ------------------------------------------------
echo . usx:   %usx%
echo . folder: %2
echo . output: %2\convert\%1.step3.xml
echo . input:  %2\source\%1
echo . xsl:    XSLT\step3
if *%usx%*==*true* goto usx3
"C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\%1.step3.xml c:\d\Kahuna\%2\source\%1 XSLT\step3.xsl pathToFiles="C:\d\kahuna\" bookNamesFile="listOfBookNames.html"  bookNamesXml="BookNames.xml"
goto end3
:usx3
for /f %%f in ('dir /b c:\d\Kahuna\%2\source\*.usx') do call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\%%~nf.333 c:\d\Kahuna\%2\source\%%f XSLT\step3.xsl bidi=%bidi% 


echo ------------------------------------------------


:end3
if exist %2\convert\%1.step3.xml goto step4
if exist %2\convert\*.333 goto step4
goto badEnding

rem the following two changes are needed only for Persian to fix errors in incoming file 
rem at the end we return to step 4 to continue

:Persian3fixHyphen
echo ------------------------------------------------
echo . step Persian3fixHyphen fix space around hypen error 
echo ------------------------------------------------
echo . output: %2\convert\%1.step3PersianFixHyphen.xml
echo . input:  %2\convert\%1.%2\source\%1
echo . xsl:    XSLT\stepPersian3fixHyphen
if *%usx%*==*true* goto usx3.5.Persian
"C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\%1.step3PersianFixHyphen.xml c:\d\Kahuna\%2\source\%1 XSLT\step3PersianFixHyphen.xsl pathToFiles="C:\d\kahuna\" bookNamesFile="listOfBookNames.html"  bookNamesXml="BookNames.xml"
goto step3PersianFixChapterVerse
:usx3.5.Persian
for /f %%f in ('dir /b c:\d\Kahuna\%2\source\*.usx') do call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\%%~nf.Persian3fixHyphen.xml c:\d\Kahuna\%2\source\%%f XSLT\stepPersian3fixHyphen bidi=%bidi% 

:step3PersianFixChapterVerse
echo ------------------------------------------------
echo . step Persian fix chapter verse error 
echo ------------------------------------------------
echo . output: %2\convert\%1.step3PersianFixChapterVerse.xml
echo . input:  %2\convert\%1.step3PersianFixHyphen.xml
echo . xsl:    XSLT\step3Persian
if *%usx%*==*true* goto usx3Persian
"C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\%1.step3PersianFixChapterVerse.xml %2\convert\%1.step3PersianFixHyphen.xml XSLT\step3PersianFixChapterVerse.xsl pathToFiles="C:\d\kahuna\" bookNamesFile="listOfBookNames.html"  bookNamesXml="BookNames.xml"
goto step3.Persian
:usx3Persian
for /f %%f in ('dir /b c:\d\Kahuna\%2\source\*.usx') do call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\%%~nf.stepPersian3fixHyphen.xml c:\d\Kahuna\%2\source\%%f XSLT\step3.Persian.xsl bidi=%bidi% 

rem ..... XXXXX fix the multiple path if needed

:step3.Persian
echo ------------------------------------------------
echo . step 3 - Persian mark fully identified canonical references  
echo ------------------------------------------------
echo . usx:   %usx%
echo . folder: %2
echo . output: %2\convert\%1.step3
echo . input:  %2\convert\%1.step3PersianFixChapterVerse
echo . xsl:    XSLT\step3
if *%usx%*==*true* goto usx3
"C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\%1.step3.xml %2\convert\%1.step3PersianFixChapterVerse.xml XSLT\step3.xsl pathToFiles="C:\d\kahuna\" bookNamesFile="listOfBookNames.html"  bookNamesXml="BookNames.xml"
:end3Persian
:usx3
for /f %%f in ('dir /b c:\d\Kahuna\%2\source\*.usx') do call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" %2\convert\%1.step3.xml -o:%2\convert\%%~nf.stepPersian3fixHyphen.xml XSLT\step3.xsl bidi=%bidi%


echo ------------------------------------------------

:end3Persian
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
call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\%1.step4.xml %2\convert\%1.step3.xml XSLT\step4.xsl
goto end4
:usx4
for /f %%f in ('dir /b c:\d\Kahuna\%2\convert\*.333') do call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\%%~nf.444 c:\d\Kahuna\%2\convert\%%f XSLT\step4.xsl 

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
call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\%1.step5.xml %2\convert\%1.step4.xml XSLT\step5.xsl
goto end5
:usx5
for /f %%f in ('dir /b c:\d\Kahuna\%2\convert\*.444') do call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\%%~nf.usx c:\d\Kahuna\%2\convert\%%f XSLT\step5.xsl 

echo ------------------------------------------------

:end5
if exist %2\convert\%1.step5.xml  goto step6
if exist %2\convert\*.555 goto :step6
goto badEnding

:step6
echo . step 5 completed
echo ------------------------------------------------
echo . step 6 - OBA 8.1 fix  
echo ------------------------------------------------
echo . output: %2\convert\%1.step6.xml
echo . input:  %2\convert\%1.step5.xml
echo . xsl:    XSLT\step6


if *%usx%*==*true* goto usx6
call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\%1.step6.xml %2\convert\%1.step5.xml XSLT\step6.xsl
goto end6
:usx6
for /f %%f in ('dir /b c:\d\Kahuna\%2\convert\*.555') do call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\%%~nf.usx c:\d\Kahuna\%2\convert\%%f XSLT\step6.xsl 

echo ------------------------------------------------

:end6
echo . step 6 completed
echo ------------------------------------------------
if exist %2\convert\%1.step6.xml  goto step7
if exist %2\convert\*.666 goto :step7
goto end

:step7
:validate
echo . NOT USING STEP 7... STOP at 6 except for validation test.

echo %time% >> %2\convert\%1.timer.txt 
echo ------------------------------------------------
echo . step 7 - validate %1
echo ------------------------------------------------
echo . output: %2\convert\%1.step7.xml
echo . input:  %2\convert\%1.step6.xml
echo . xsl:    XSLT\validate.xsl
call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\%1.step7.xml %2\convert\%1.step6.xml XSLT\step7.xsl
echo %time% >> %2\convert\%1.timer.txt 


:step8
if *%2*==*sample* goto :validate2
goto :end
:validate2
echo ------------------------------------------------
echo . step 8 - validate sample
echo ------------------------------------------------
echo . output: %2\convert\sampleRefs.validate.usx
echo . input:  %2\convert\sampleRefs.usx
echo . xsl:    XSLT\validate.xsl

call "C:\Program Files\saxonica\saxon960HE\saxon9he.jar" -o:%2\convert\sampleRefs.validate.usx %2\convert\%1.step6.xml XSLT\validate.xsl

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
pause