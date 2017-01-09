@rem  Edit the following lines so that they work, if necessary.
SET DEVENV="C:\Program Files\Microsoft Visual Studio 9.0\Common7\IDE\devenv.com"
SET NSIS="c:\program files\nsis\makensis.exe"

interval start

set disttype=debug
cd UpdateVersion
%DEVENV% UpdateVersion.csproj /build %disttype% /nologo
cd bin\%disttype%\
UpdateVersion.exe
cd ..\..\..
xcopy /d /y doc\* usfm2word\bin\%disttype%
xcopy /d /y *.xml usfm2word\bin\%disttype%
xcopy /d /y SFMInfo.xml extract-usfx\bin\%disttype%
xcopy /d /y BibleBookInfo.xml extract-usfx\bin\%disttype%
xcopy /d /y *.xsd usfm2word\bin\%disttype%
xcopy /d /y SFMInfo.xml usfm2usfx\bin\%disttype%
xcopy /d /y BibleBookInfo.xml usfm2usfx\bin\%disttype%
xcopy /d /y SFMInfo.xml usfx2usfm\bin\%disttype%
xcopy /d /y BibleBookInfo.xml usfx2usfm\bin\%disttype%
xcopy /d /y *.xsd usfm2usfx\bin\%disttype%
%DEVENV% wordsend.sln /rebuild %disttype% /nologo

set disttype=release
cd UpdateVersion
%DEVENV% UpdateVersion.csproj /build %disttype% /nologo
cd bin\%disttype%\
UpdateVersion.exe
cd ..\..\..
xcopy /d /y doc\* usfm2word\bin\%disttype%
xcopy /d /y *.xml usfm2word\bin\%disttype%
xcopy /d /y *.xsd usfm2word\bin\%disttype%
xcopy /d /y SFMInfo.xml usfm2usfx\bin\%disttype%
xcopy /d /y BibleBookInfo.xml usfm2usfx\bin\%disttype%
xcopy /d /y SFMInfo.xml usfx2usfm\bin\%disttype%
xcopy /d /y BibleBookInfo.xml usfx2usfm\bin\%disttype%
xcopy /d /y *.xsd usfm2usfx\bin\%disttype%
xcopy /d /y doc\* dist\
%DEVENV% wordsend.sln /rebuild %disttype% /nologo
delete /s *.wbk *.bak dist\wordsend-setup.zip dist\wordsend-source.zip dist\wordsend-console.zip

rem Change to the directory just above wordsend.
REM cd ..
REM zip -X -9 -r wordsend\dist\wordsend-source.zip wordsend\* -x *\bin\* *\obj\* *.user *.suo wordsend\dist\* *.bak *.tmp

REM cd WordSend
echo simulated delete /rf wordsend\*
copy /y doc\* wordsend\
copy /y *.xml wordsend\
copy /y *.xsd wordsend\
xcopy /d /y BibleFileLib\bin\Release\* wordsend\
xcopy /d /y sf2word\bin\Release\* wordsend\
xcopy /d /y usfm2usfx\bin\Release\* wordsend\
xcopy /d /y usfm2usfx\App.ico wordsend\
xcopy /d /y extract-usfx\bin\Release\* wordsend\
xcopy /d /y usfx2usfm\bin\Release\* wordsend\
xcopy /d /y usfm2word\bin\Release\* wordsend\
zip -X -9 -r dist\wordsend-console.zip wordsend\*

cd installer
%NSIS% wordsend.nsi
cd ..
call VersionStamp.bat
dir dist\*
interval stop
