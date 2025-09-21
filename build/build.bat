set QTDIR=D:\Qt\Qt5.12.12\5.12.12\msvc2017_64
set PATH="%QTDIR%\bin";%PATH%
qmake ../apkstudio.pro CONFIG+=release ../../apkstudio
nmake
windeployqt release\ApkStudio.exe