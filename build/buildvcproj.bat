set QTDIR=D:\Qt\6.9.2\msvc2022_64
set PATH="%QTDIR%\bin";%PATH%
qmake -tp vc ../apkstudio.pro
rem qmake ../apkstudio.pro CONFIG+=debug ../../apkstudio
rem nmake
rem windeployqt debug\ApkStudio.exe