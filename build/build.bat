set QTDIR=D:\Qt\6.9.2\msvc2022_64
set PATH="%QTDIR%\bin";%PATH%
qmake ../apkstudio.pro CONFIG+=release ../../apkstudio
nmake
windeployqt release\ApkStudio.exe