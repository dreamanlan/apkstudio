#include <QDebug>
#include "apkdecompileworker.h"
#include "processutils.h"
#include "HostCLR.h"

ApkDecompileWorker::ApkDecompileWorker(const QString &apk, const QString &folder, const bool smali, const bool resources, const bool java, QObject *parent)
    : QObject(parent), m_Apk(apk), m_Folder(folder), m_Java(java), m_Resources(resources), m_Smali(smali)
{
}

void ApkDecompileWorker::decompile()
{
    emit started();
#ifdef QT_DEBUG
    qDebug() << "Decompiling" << m_Apk << "into" << m_Folder;
#endif
    ProcessResult result{};
    if (decompile_apk_fptr) {
        std::string s = m_Apk.toStdString();
        std::string s2 = m_Folder.toStdString();
        int r = decompile_apk_fptr(s.c_str(), s2.c_str(), m_Java, m_Resources, m_Smali, this, &result);
#ifdef QT_DEBUG
        qDebug() << "CSharp decompile apk returned code" << r;
#endif
        if (r > 0) {
            emit decompileFinished(m_Apk, m_Folder);
            emit finished();
            return;
        }
        else if (r < 0) {
            emit decompileFailed(m_Apk);
            return;
        }
    }
    const QString java = ProcessUtils::javaExe();
    const QString apktool = ProcessUtils::apktoolJar();
    if (java.isEmpty() || apktool.isEmpty()) {
        emit decompileFailed(m_Apk);
        return;
    }
    emit decompileProgress(25, tr("Running apktool..."));
    QString heap("-Xmx%1m");
    heap = heap.arg(QString::number(ProcessUtils::javaHeapSize()));
    QStringList args;
    args << heap << "-jar" << apktool;
    args << "d";
    if (!m_Smali) {
        args << "-s";
    }
    if (!m_Resources) {
        args << "-r";
    }
    args << "-o" << m_Folder << m_Apk;
    result = ProcessUtils::runCommand(java, args);
#ifdef QT_DEBUG
    qDebug() << "Apktool returned code" << result.code;
#endif
    if (result.code != 0) {
        emit decompileFailed(m_Apk);
        return;
    }
    if (m_Java) {
        emit decompileProgress(75, tr("Running jadx..."));
        const QString jadx = ProcessUtils::jadxExe();
        if (jadx.isEmpty()) {
            emit decompileFailed(m_Apk);
            return;
        }
        args.clear();
        args << "-r" << "-d" << m_Folder << m_Apk;
        result = ProcessUtils::runCommand(jadx, args, PROCESS_TIMEOUT_SECS);
#ifdef QT_DEBUG
        qDebug() << "Jadx returned code" << result.code;
#endif
    }
    emit decompileFinished(m_Apk, m_Folder);
    emit finished();
}