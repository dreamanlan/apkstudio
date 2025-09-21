#include <QDebug>
#include "apkrecompileworker.h"
#include "processutils.h"
#include "HostCLR.h"

ApkRecompileWorker::ApkRecompileWorker(const QString &folder, bool aapt2, QObject *parent)
    : QObject(parent), m_Aapt2(aapt2), m_Folder(folder)
{
}

void ApkRecompileWorker::recompile()
{
    emit started();
#ifdef QT_DEBUG
    qDebug() << "Recompiling" << m_Folder;
#endif
    ProcessResult result{};
    if (recompile_apk_fptr) {
        std::string s = m_Folder.toStdString();
        int r = recompile_apk_fptr(s.c_str(), this, &result);
#ifdef QT_DEBUG
        qDebug() << "CSharp recompile apk returned code" << r;
#endif
        if (r > 0) {
            emit recompileFinished(m_Folder);
            emit finished();
            return;
        }
        else if (r < 0) {
            emit recompileFailed(m_Folder);
            return;
        }
    }
    const QString java = ProcessUtils::javaExe();
    const QString apktool = ProcessUtils::apktoolJar();
    const QString zipalign = ProcessUtils::zipalignExe();
    if (java.isEmpty() || apktool.isEmpty() || zipalign.isEmpty()) {
        emit recompileFailed(m_Folder);
        return;
    }
    QString heap("-Xmx%1m");
    heap = heap.arg(QString::number(ProcessUtils::javaHeapSize()));
    QStringList args;
    args << heap << "-jar" << apktool;
    args << "b" << m_Folder;
    if (m_Aapt2) {
        args << "--use-aapt2";
    }
    result = ProcessUtils::runCommand(java, args);
#ifdef QT_DEBUG
    qDebug() << "Apktool returned code" << result.code;
#endif
    if (result.code != 0) {
        emit recompileFailed(m_Folder);
        return;
    }
    if (get_zipalign_args_fptr) {
        char zipalignArgs[1025] = "-P 16 -f -v 4";
        int len = 1024;
        std::string s = m_Folder.toStdString();
        get_zipalign_args_fptr(s.c_str(), zipalignArgs, len);
        result = ProcessUtils::runCommand(zipalign, QString(zipalignArgs).split(' '));
#ifdef QT_DEBUG
        qDebug() << "Zipalign returned code" << result.code;
#endif
        if (result.code != 0) {
            emit recompileFailed(m_Folder);
            return;
        }
    }
    emit recompileFinished(m_Folder);
    emit finished();
}